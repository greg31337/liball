namespace uspto;

internal class TrademarkDailyXmlRecord
{
    internal long RecordNumber = 0L;
    internal string SourcePath = "/";
    internal string SchemaGeneration = string.Empty;
    internal string? TransactionDateText = null;
    internal TrademarkCaseStatus CaseStatus = new TrademarkCaseStatus();
    internal string RawXml = string.Empty;
    internal global::System.Collections.Generic.Dictionary<string, string> RawValues = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
    internal PatentDataProvenance Provenance = new PatentDataProvenance();
}

internal class TrademarkDailyStatement
{
    internal string? TypeCode = null;
    internal string? Text = null;
}

internal static class TrademarkDailyXmlParser
{
    internal static TrademarkDailyXmlRecord ParseRecord(XmlRecord record, PatentDataProvenance provenance)
    {
        if (record == null)
        {
            throw new ValidationException("A trademark daily XML record is required.", "record", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Trademark daily XML provenance is required.", "provenance", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(record.RawXml))
        {
            throw new ValidationException("The trademark daily XML record must retain its raw subtree.", "RawXml", "REQUIRED", null);
        }

        global::System.Xml.XmlElement documentRoot = LoadTrademarkRoot(record.RawXml);
        global::System.Xml.XmlElement? caseFile = ResolveCaseFile(documentRoot);
        if (caseFile == null)
        {
            throw DailyFailure("The XML record is not a trademark application or status case file.", record.Path, record);
        }

        global::System.Xml.XmlElement? header = FindFirstDescendant(caseFile, new string[] { "case-file-header", "caseFileHeader", "status" });
        if (header == null)
        {
            header = caseFile;
        }

        PatentDataProvenance copiedProvenance = OdpJsonParser.CopyProvenance(provenance);
        copiedProvenance.Source = DataProvenance.Bulk;
        copiedProvenance.IsAuthoritative = true;

        TrademarkCaseStatus status = new TrademarkCaseStatus();
        status.RawFormat = DocumentFormat.Xml;
        status.RawResponse = record.RawXml;
        status.Provenance = copiedProvenance;
        status.IsComplete = true;
        ApplyDailyIdentity(status, caseFile, header, record);
        ApplyDailyStatus(status.Status, header);
        TrademarkDailyStatement[] statements = ParseStatements(caseFile);
        ApplyDailyMark(status.Mark, caseFile, header, statements);
        ApplyDailyBasis(status.FilingBasis, header);
        status.CurrentOwners = ParseOwnerGroup(caseFile, true);
        status.PriorOwners = ParseOwnerGroup(caseFile, false);
        status.ProsecutionEvents = ParseDailyEvents(caseFile);
        status.GoodsAndServices = ParseDailyGoods(caseFile, status.FilingBasis, statements, status.SerialNumber);

        TrademarkDailyXmlRecord result = new TrademarkDailyXmlRecord();
        result.RecordNumber = record.RecordNumber;
        result.SourcePath = record.Path;
        result.SchemaGeneration = ResolveSchemaGeneration(documentRoot, caseFile);
        result.TransactionDateText = ReadFirstText(caseFile, new string[] { "transaction-date", "transactionDate", "file-date", "daily-date" });
        result.CaseStatus = status;
        result.RawXml = record.RawXml;
        result.Provenance = copiedProvenance;
        CaptureDailyRawValues(caseFile, "/" + caseFile.LocalName, result.RawValues);
        return result;
    }

    internal static global::System.Xml.XmlElement LoadTrademarkRoot(string xml)
    {
        global::System.Xml.XmlReaderSettings settings = new global::System.Xml.XmlReaderSettings();
        settings.DtdProcessing = global::System.Xml.DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        global::System.Xml.XmlDocument document = new global::System.Xml.XmlDocument();
        document.XmlResolver = null;
        using (global::System.IO.StringReader input = new global::System.IO.StringReader(xml))
        using (global::System.Xml.XmlReader reader = global::System.Xml.XmlReader.Create(input, settings))
        {
            document.Load(reader);
        }

        if (document.DocumentElement == null)
        {
            throw new ParseException("The trademark daily XML record has no root element.", DocumentFormat.Xml, "trademark daily XML", "/", 0L, false, 0, false, null);
        }

        return document.DocumentElement;
    }

    internal static global::System.Xml.XmlElement? ResolveCaseFile(global::System.Xml.XmlElement root)
    {
        if (DailyNameMatches(root, new string[] { "case-file", "trademark-case-file", "trademark", "trademark-record" }))
        {
            return root;
        }

        if (DailyNameMatches(root, new string[] { "trademark-transaction", "trademark-case-files", "case-file-wrapper", "transaction" }))
        {
            return FindFirstDescendant(root, new string[] { "case-file", "trademark-case-file", "trademark", "trademark-record" });
        }

        return null;
    }

    internal static void ApplyDailyIdentity(TrademarkCaseStatus result, global::System.Xml.XmlElement caseFile, global::System.Xml.XmlElement header, XmlRecord record)
    {
        string? serial = ReadDirectThenDescendantText(caseFile, header, new string[] { "serial-number", "serialNumber", "serial-num" });
        if (global::System.String.IsNullOrWhiteSpace(serial))
        {
            throw DailyFailure("The trademark daily case file has no serial number.", record.Path + "/serial-number", record);
        }

        TrademarkIdentifierResult parsed = TrademarkIdentifierParser.ParseTrademarkIdentifier("sn" + serial);
        if (!parsed.IsValid)
        {
            throw DailyFailure("The trademark daily case file serial number is invalid.", record.Path + "/serial-number", record);
        }

        result.RequestedIdentifier = serial;
        result.CanonicalIdentifier = parsed.CanonicalNumber;
        result.IdentifierKind = parsed.Kind;
        result.SerialNumber = parsed.IdentifierValue;
        result.RegistrationNumber = ReadDirectThenDescendantText(caseFile, header, new string[] { "registration-number", "us-registration-number", "registrationNumber", "registration-num" });
    }

    internal static void ApplyDailyStatus(TrademarkStatus result, global::System.Xml.XmlElement header)
    {
        string? codeText = ReadFirstText(header, new string[] { "status-code", "statusCode", "status" });
        result.CodeText = codeText;
        TsdrXmlParser.ApplyStatusInteger(result, codeText, 1);
        result.ExternalDescription = ReadFirstText(header, new string[] { "status-description", "external-status-description", "ext-status-desc" });
        result.InternalDescription = ReadFirstText(header, new string[] { "internal-status-description", "int-status-desc" });
        TsdrXmlParser.ApplyStatusInteger(result, ReadFirstText(header, new string[] { "tm5-status-code", "tm5Status" }), 2);
        result.Tm5Description = ReadFirstText(header, new string[] { "tm5-status-description", "tm5StatusDesc" });
        result.Tm5Definition = ReadFirstText(header, new string[] { "tm5-status-definition", "tm5StatusDef" });
        TsdrJsonParser.ApplyStatusDate(result, TsdrXmlParser.ParseDate(ReadFirstText(header, new string[] { "status-date", "statusDate" })), 1);
        TsdrJsonParser.ApplyStatusDate(result, TsdrXmlParser.ParseDate(ReadFirstText(header, new string[] { "filing-date", "application-date", "filingDate" })), 2);
        TsdrJsonParser.ApplyStatusDate(result, TsdrXmlParser.ParseDate(ReadFirstText(header, new string[] { "registration-date", "us-registration-date", "registrationDate" })), 3);
        TsdrJsonParser.ApplyStatusDate(result, TsdrXmlParser.ParseDate(ReadFirstText(header, new string[] { "abandonment-date", "date-abandoned" })), 4);
        TsdrJsonParser.ApplyStatusDate(result, TsdrXmlParser.ParseDate(ReadFirstText(header, new string[] { "cancellation-date", "date-cancelled" })), 5);
        TsdrJsonParser.ApplyStatusDate(result, TsdrXmlParser.ParseDate(ReadFirstText(header, new string[] { "renewal-date", "date-renewal" })), 6);
        result.RegisterCategory = ReadFirstText(header, new string[] { "register-category", "register" });
        if (result.RegisterCategory == null && ReadDailyBoolean(ReadFirstText(header, new string[] { "principal-register-amended-in", "principal-register-in" })) == 1)
        {
            result.RegisterCategory = "Principal";
        }

        if (result.RegisterCategory == null && ReadDailyBoolean(ReadFirstText(header, new string[] { "supplemental-register-amended-in", "supplemental-register-in" })) == 1)
        {
            result.RegisterCategory = "Supplemental";
        }

        result.LawOfficeCode = ReadFirstText(header, new string[] { "law-office-assigned", "law-office-code", "lawOffAsgnCd" });
        result.CurrentLocationCode = ReadFirstText(header, new string[] { "current-location", "current-location-code", "currLocationCd" });
        result.CurrentLocationDescription = ReadFirstText(header, new string[] { "current-location-description", "currentLoc" });
        TsdrXmlParser.ApplyStatusInteger(result, ReadFirstText(header, new string[] { "active-class-count", "class-count-active", "clsCtActive" }), 3);
        TsdrXmlParser.ApplyStatusInteger(result, ReadFirstText(header, new string[] { "total-class-count", "class-count-total", "clsTotal" }), 4);
    }

    internal static void ApplyDailyMark(TrademarkMark result, global::System.Xml.XmlElement caseFile, global::System.Xml.XmlElement header, TrademarkDailyStatement[] statements)
    {
        result.VerbalElementText = ReadFirstText(header, new string[] { "mark-identification", "mark-element", "mark-verbal-element-text", "word-mark" });
        result.Type = ReadFirstText(header, new string[] { "mark-type", "markType" });
        result.DrawingCode = ReadFirstText(header, new string[] { "mark-drawing-code", "drawing-code", "markDrawingCd" });
        result.DrawingDescription = ReadFirstText(header, new string[] { "mark-drawing-description", "drawing-description", "markDrawDesc" });
        result.FeatureCode = ReadFirstText(header, new string[] { "mark-feature-code", "feature-code" });
        result.FeatureDescription = ReadFirstText(header, new string[] { "mark-feature-description", "feature-description" });
        ApplyMarkIndicator(result, ReadFirstText(header, new string[] { "standard-character-claim", "standard-character-in", "standardChar" }), 1);
        ApplyMarkIndicator(result, ReadFirstText(header, new string[] { "color-drawing-current-in", "color-drawing-in", "colorDrawingCurr" }), 2);
        result.Description = ReadFirstText(header, new string[] { "description-of-mark", "mark-description", "descOfMark" });
        result.ColorClaimText = ReadFirstText(header, new string[] { "color-claim", "color-claimed", "colorClaimText" });
        result.ColorLocationText = ReadFirstText(header, new string[] { "color-location", "colorLocationText" });
        result.DisclaimerText = ReadFirstText(header, new string[] { "disclaimer", "disclaimer-text" });
        result.PseudoMarkText = ReadFirstText(header, new string[] { "pseudo-mark", "pseudo-mark-text" });
        result.TranslationText = ReadFirstText(header, new string[] { "translation", "translation-text" });
        result.TransliterationText = ReadFirstText(header, new string[] { "transliteration", "transliteration-text" });
        ApplyMarkStatements(result, statements);
        result.DesignSearchClassifications = ParseDailyDesignSearch(caseFile);
    }

    internal static void ApplyMarkIndicator(TrademarkMark target, string? text, int field)
    {
        int value = ReadDailyBoolean(text);
        if (value >= 0)
        {
            if (field == 1)
            {
                target.IsStandardCharacter = value == 1;
                target.HasIsStandardCharacter = true;
            }
            else
            {
                target.IsColorDrawing = value == 1;
                target.HasIsColorDrawing = true;
            }
        }
    }

    internal static TrademarkDailyStatement[] ParseStatements(global::System.Xml.XmlElement caseFile)
    {
        global::System.Xml.XmlElement? container = FindFirstDescendant(caseFile, new string[] { "case-file-statements", "statements" });
        if (container == null)
        {
            return new TrademarkDailyStatement[0];
        }

        global::System.Collections.Generic.List<global::System.Xml.XmlElement> elements = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        CollectDescendants(container, new string[] { "case-file-statement", "statement" }, elements);
        TrademarkDailyStatement[] result = new TrademarkDailyStatement[elements.Count];
        int index;
        for (index = 0; index < elements.Count; index += 1)
        {
            TrademarkDailyStatement statement = new TrademarkDailyStatement();
            statement.TypeCode = ReadFirstText(elements[index], new string[] { "type-code", "statement-type-code", "code" });
            statement.Text = ReadFirstText(elements[index], new string[] { "text", "statement-text", "description" });
            result[index] = statement;
        }

        return result;
    }

    internal static void ApplyMarkStatements(TrademarkMark mark, TrademarkDailyStatement[] statements)
    {
        int index;
        for (index = 0; index < statements.Length; index += 1)
        {
            string code = NormalizeDailyName(statements[index].TypeCode ?? string.Empty);
            string? text = statements[index].Text;
            if (text == null)
            {
                continue;
            }

            if (code.StartsWith("dm", global::System.StringComparison.Ordinal) && mark.Description == null)
            {
                mark.Description = text;
            }
            else if (code.StartsWith("cc", global::System.StringComparison.Ordinal) && mark.ColorClaimText == null)
            {
                mark.ColorClaimText = text;
            }
            else if (code.StartsWith("cd", global::System.StringComparison.Ordinal) && mark.ColorLocationText == null)
            {
                mark.ColorLocationText = text;
            }
            else if (code.StartsWith("d1", global::System.StringComparison.Ordinal) && mark.DisclaimerText == null)
            {
                mark.DisclaimerText = text;
            }
            else if (code.StartsWith("pm", global::System.StringComparison.Ordinal) && mark.PseudoMarkText == null)
            {
                mark.PseudoMarkText = text;
            }
            else if (code.StartsWith("tr", global::System.StringComparison.Ordinal) && mark.TranslationText == null)
            {
                mark.TranslationText = text;
            }
            else if (code.StartsWith("tl", global::System.StringComparison.Ordinal) && mark.TransliterationText == null)
            {
                mark.TransliterationText = text;
            }
        }
    }

    internal static TrademarkDesignSearchClassification[] ParseDailyDesignSearch(global::System.Xml.XmlElement caseFile)
    {
        global::System.Xml.XmlElement? container = FindFirstDescendant(caseFile, new string[] { "case-file-design-search-codes", "case-file-design-searches", "design-search-list", "design-searches" });
        if (container == null)
        {
            return new TrademarkDesignSearchClassification[0];
        }

        global::System.Collections.Generic.List<global::System.Xml.XmlElement> elements = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        CollectDescendants(container, new string[] { "case-file-design-search-code", "case-file-design-search", "design-search", "design-search-code" }, elements);
        TrademarkDesignSearchClassification[] result = new TrademarkDesignSearchClassification[elements.Count];
        int index;
        for (index = 0; index < elements.Count; index += 1)
        {
            TrademarkDesignSearchClassification value = new TrademarkDesignSearchClassification();
            value.Code = ReadFirstText(elements[index], new string[] { "design-search-code", "code" });
            value.Descriptions = ReadTextCollection(elements[index], new string[] { "description", "design-description" });
            result[index] = value;
        }

        return result;
    }

    internal static void ApplyDailyBasis(TrademarkFilingBasis result, global::System.Xml.XmlElement source)
    {
        ApplyBasisIndicator(result, ReadFirstText(source, new string[] { "filed-use-in", "use-current-in", "filed-use", "section-1a" }), 1);
        ApplyBasisIndicator(result, ReadFirstText(source, new string[] { "filed-itu-in", "itu-current-in", "filed-itu", "section-1b" }), 2);
        ApplyBasisIndicator(result, ReadFirstText(source, new string[] { "filed-44d-in", "section-44d" }), 3);
        ApplyBasisIndicator(result, ReadFirstText(source, new string[] { "filed-44e-in", "section-44e" }), 4);
        ApplyBasisIndicator(result, ReadFirstText(source, new string[] { "filed-66a-in", "section-66a" }), 5);
    }

    internal static void ApplyBasisIndicator(TrademarkFilingBasis target, string? text, int field)
    {
        int value = ReadDailyBoolean(text);
        if (value >= 0)
        {
            TsdrXmlParser.ApplyBasisBoolean(target, value == 1 ? "Y" : "N", field);
        }
    }

    internal static TrademarkOwner[] ParseOwnerGroup(global::System.Xml.XmlElement caseFile, bool current)
    {
        string[] containerNames = current
            ? new string[] { "case-file-owners", "current-owners", "owners" }
            : new string[] { "case-file-prior-owners", "prior-owner-list", "prior-owners" };
        global::System.Xml.XmlElement? container = FindFirstDescendant(caseFile, containerNames);
        if (container == null)
        {
            return new TrademarkOwner[0];
        }

        global::System.Collections.Generic.List<global::System.Xml.XmlElement> elements = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        CollectDescendants(container, new string[] { "case-file-owner", "owner", "party" }, elements);
        TrademarkOwner[] result = new TrademarkOwner[elements.Count];
        int index;
        for (index = 0; index < elements.Count; index += 1)
        {
            global::System.Xml.XmlElement element = elements[index];
            TrademarkOwner owner = new TrademarkOwner();
            TsdrXmlParser.ApplyOwnerInteger(owner, ReadFirstText(element, new string[] { "entry-number", "sequence-number", "entity-num" }), 1);
            TsdrXmlParser.ApplyOwnerInteger(owner, ReadFirstText(element, new string[] { "party-type", "party-type-code" }), 2);
            owner.PartyTypeDescription = ReadFirstText(element, new string[] { "party-type-description", "party-type-string" });
            owner.Name = ReadFirstText(element, new string[] { "owner-name", "name", "legal-entity-name" });
            owner.EntityTypeCode = ReadFirstText(element, new string[] { "entity-type-code", "legal-entity-type-code", "entity-code" });
            owner.EntityTypeDescription = ReadFirstText(element, new string[] { "entity-type-description" });
            owner.AlternateNameText = ReadFirstText(element, new string[] { "dba-aka-formerly", "dba-aka-text", "alternate-name-text" });
            owner.CompositionText = ReadFirstText(element, new string[] { "composed-of", "entity-statement", "ownership-composition" });
            owner.AssignmentReelFrame = ReadFirstText(element, new string[] { "reel-frame", "assignment-reel-frame" });
            owner.CitizenshipCountryCode = ReadFirstText(element, new string[] { "nationality-country", "nationality", "citizenship-country-code" });
            owner.CitizenshipCountryName = ReadFirstText(element, new string[] { "citizenship-country-name" });
            owner.Address = ParseDailyAddress(element);
            owner.EmailAddresses = ReadTextCollection(element, new string[] { "email-address", "email" });
            owner.IsCurrent = current;
            owner.HasIsCurrent = true;
            int currentIndicator = ReadDailyBoolean(ReadFirstText(element, new string[] { "current-owner-in", "is-current" }));
            if (currentIndicator >= 0)
            {
                owner.IsCurrent = currentIndicator == 1;
            }

            result[index] = owner;
        }

        return result;
    }

    internal static TrademarkAddress ParseDailyAddress(global::System.Xml.XmlElement source)
    {
        TrademarkAddress result = new TrademarkAddress();
        result.Line1 = ReadFirstText(source, new string[] { "address-1", "address-line-1", "line-1", "address1" });
        result.Line2 = ReadFirstText(source, new string[] { "address-2", "address-line-2", "line-2", "address2" });
        result.City = ReadFirstText(source, new string[] { "city" });
        result.StateOrProvince = ReadFirstText(source, new string[] { "state", "state-or-province" });
        result.PostalCode = ReadFirstText(source, new string[] { "postcode", "postal-code", "zip" });
        result.CountryCode = ReadFirstText(source, new string[] { "country-code", "country" });
        result.CountryName = ReadFirstText(source, new string[] { "country-name" });
        return result;
    }

    internal static TrademarkProsecutionEvent[] ParseDailyEvents(global::System.Xml.XmlElement caseFile)
    {
        global::System.Xml.XmlElement? container = FindFirstDescendant(caseFile, new string[] { "case-file-event-statements", "prosecution-history", "events" });
        if (container == null)
        {
            return new TrademarkProsecutionEvent[0];
        }

        global::System.Collections.Generic.List<global::System.Xml.XmlElement> elements = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        CollectDescendants(container, new string[] { "case-file-event-statement", "prosecution", "prosecution-history-event", "event" }, elements);
        TrademarkProsecutionEvent[] result = new TrademarkProsecutionEvent[elements.Count];
        int index;
        for (index = 0; index < elements.Count; index += 1)
        {
            global::System.Xml.XmlElement element = elements[index];
            TrademarkProsecutionEvent value = new TrademarkProsecutionEvent();
            value.Code = ReadFirstText(element, new string[] { "code", "event-code", "entry-code", "event-action-code" });
            value.Type = ReadFirstText(element, new string[] { "type", "event-type", "entry-type" });
            value.Description = ReadFirstText(element, new string[] { "description", "entry-description", "entry-desc", "event-description" });
            TsdrXmlParser.ApplyEventInteger(value, ReadFirstText(element, new string[] { "number", "entry-number" }), 1);
            TsdrXmlParser.ApplyEventInteger(value, ReadFirstText(element, new string[] { "sequence-number", "sequence-num" }), 2);
            value.ProceedingNumber = ReadFirstText(element, new string[] { "proceeding-number", "proceeding-num" });
            value.DocumentIdentifier = ReadFirstText(element, new string[] { "document-identifier", "doc-id" });
            TsdrJsonParser.ApplyEventDate(value, TsdrXmlParser.ParseDate(ReadFirstText(element, new string[] { "date", "entry-date", "event-date", "event-date-time" })), 1);
            TsdrJsonParser.ApplyEventDate(value, TsdrXmlParser.ParseDate(ReadFirstText(element, new string[] { "due-date" })), 2);
            TsdrJsonParser.ApplyEventDate(value, TsdrXmlParser.ParseDate(ReadFirstText(element, new string[] { "recordal-date" })), 3);
            result[index] = value;
        }

        return result;
    }

    internal static TrademarkGoodsAndService[] ParseDailyGoods(global::System.Xml.XmlElement caseFile, TrademarkFilingBasis caseBasis, TrademarkDailyStatement[] statements, string? serialNumber)
    {
        global::System.Xml.XmlElement? container = FindFirstDescendant(caseFile, new string[] { "case-file-classifications", "goods-and-services", "gs-list" });
        if (container == null)
        {
            return new TrademarkGoodsAndService[0];
        }

        global::System.Collections.Generic.List<global::System.Xml.XmlElement> elements = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        CollectDescendants(container, new string[] { "case-file-classification", "goods-and-service", "classification" }, elements);
        TrademarkGoodsAndService[] result = new TrademarkGoodsAndService[elements.Count];
        int index;
        for (index = 0; index < elements.Count; index += 1)
        {
            global::System.Xml.XmlElement element = elements[index];
            TrademarkGoodsAndService value = new TrademarkGoodsAndService();
            value.SerialNumber = serialNumber;
            value.PrimeClassCode = ReadFirstText(element, new string[] { "primary-code", "prime-class-code", "international-code-total-no", "international-code" });
            value.Description = ReadFirstText(element, new string[] { "identification", "classification-description", "description" });
            if (value.Description == null)
            {
                value.Description = FindGoodsStatement(statements, value.PrimeClassCode);
            }

            value.StatusCode = ReadFirstText(element, new string[] { "status-code", "class-status-code" });
            value.StatusDescription = ReadFirstText(element, new string[] { "status-description", "class-status-description" });
            TsdrJsonParser.ApplyGoodsDate(value, TsdrXmlParser.ParseDate(ReadFirstText(element, new string[] { "status-date", "class-status-date" })), 1);
            TsdrJsonParser.ApplyGoodsDate(value, TsdrXmlParser.ParseDate(ReadFirstText(element, new string[] { "first-use-anywhere-date", "first-use-date" })), 2);
            TsdrJsonParser.ApplyGoodsDate(value, TsdrXmlParser.ParseDate(ReadFirstText(element, new string[] { "first-use-in-commerce-date" })), 3);
            value.FirstUseDateDescription = ReadFirstText(element, new string[] { "first-use-anywhere-date-description", "first-use-date-description" });
            value.FirstUseInCommerceDateDescription = ReadFirstText(element, new string[] { "first-use-in-commerce-date-description" });
            value.Classes = ParseDailyClasses(element);
            CopyDailyBasis(value.FilingBasis, caseBasis);
            ApplyDailyBasis(value.FilingBasis, element);
            result[index] = value;
        }

        return result;
    }

    internal static string? FindGoodsStatement(TrademarkDailyStatement[] statements, string? classCode)
    {
        int index;
        for (index = 0; index < statements.Length; index += 1)
        {
            string code = NormalizeDailyName(statements[index].TypeCode ?? string.Empty);
            if (code.StartsWith("gs", global::System.StringComparison.Ordinal))
            {
                if (classCode == null || code.Contains(NormalizeDailyName(classCode), global::System.StringComparison.Ordinal))
                {
                    return statements[index].Text;
                }
            }
        }

        return null;
    }

    internal static TrademarkClass[] ParseDailyClasses(global::System.Xml.XmlElement classification)
    {
        global::System.Collections.Generic.List<TrademarkClass> results = new global::System.Collections.Generic.List<TrademarkClass>();
        string[] international = ReadTextCollection(classification, new string[] { "international-code-total-no", "international-code", "international-class-code" });
        AddDailyClasses(results, international, "international");
        string[] unitedStates = ReadTextCollection(classification, new string[] { "us-code-total-no", "us-code", "us-class-code" });
        AddDailyClasses(results, unitedStates, "us");
        AddNestedDailyClasses(results, classification, new string[] { "international-classes" }, "international");
        AddNestedDailyClasses(results, classification, new string[] { "us-classes" }, "us");
        return results.ToArray();
    }

    internal static void AddNestedDailyClasses(global::System.Collections.Generic.List<TrademarkClass> destination, global::System.Xml.XmlElement classification, string[] containerNames, string category)
    {
        global::System.Xml.XmlElement? container = FindFirstDescendant(classification, containerNames);
        if (container == null)
        {
            return;
        }

        global::System.Collections.Generic.List<global::System.Xml.XmlElement> elements = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        CollectDescendants(container, new string[] { "tm-class", "class" }, elements);
        int index;
        for (index = 0; index < elements.Count; index += 1)
        {
            string? code = ReadFirstText(elements[index], new string[] { "code", "class-number" });
            if (code != null)
            {
                TrademarkClass value = new TrademarkClass();
                value.Category = category;
                value.Code = code;
                value.Description = ReadFirstText(elements[index], new string[] { "description", "class-title-text" });
                destination.Add(value);
            }
        }
    }

    internal static void AddDailyClasses(global::System.Collections.Generic.List<TrademarkClass> destination, string[] codes, string category)
    {
        int index;
        for (index = 0; index < codes.Length; index += 1)
        {
            TrademarkClass value = new TrademarkClass();
            value.Category = category;
            value.Code = codes[index];
            destination.Add(value);
        }
    }

    internal static void CopyDailyBasis(TrademarkFilingBasis target, TrademarkFilingBasis source)
    {
        target.Section1A = source.Section1A;
        target.HasSection1A = source.HasSection1A;
        target.Section1B = source.Section1B;
        target.HasSection1B = source.HasSection1B;
        target.Section44D = source.Section44D;
        target.HasSection44D = source.HasSection44D;
        target.Section44E = source.Section44E;
        target.HasSection44E = source.HasSection44E;
        target.Section66A = source.Section66A;
        target.HasSection66A = source.HasSection66A;
    }

    internal static string ResolveSchemaGeneration(global::System.Xml.XmlElement documentRoot, global::System.Xml.XmlElement caseFile)
    {
        string? version = ReadAttributeValue(documentRoot, new string[] { "schema-version", "version", "dtd-version" });
        if (version == null && documentRoot != caseFile)
        {
            version = ReadAttributeValue(caseFile, new string[] { "schema-version", "version", "dtd-version" });
        }

        if (version != null)
        {
            return version;
        }

        if (DailyNameMatches(caseFile, new string[] { "case-file", "trademark-case-file" }))
        {
            return "case-file";
        }

        return "transaction";
    }

    internal static string? ReadAttributeValue(global::System.Xml.XmlElement element, string[] names)
    {
        foreach (global::System.Xml.XmlAttribute attribute in element.Attributes)
        {
            string normalized = NormalizeDailyName(attribute.LocalName);
            int index;
            for (index = 0; index < names.Length; index += 1)
            {
                if (normalized.Equals(NormalizeDailyName(names[index]), global::System.StringComparison.Ordinal))
                {
                    string value = attribute.Value.Trim();
                    if (value.Length > 0)
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    internal static string? ReadDirectThenDescendantText(global::System.Xml.XmlElement primary, global::System.Xml.XmlElement fallback, string[] names)
    {
        string? result = ReadDirectText(primary, names);
        if (result == null)
        {
            result = ReadFirstText(fallback, names);
        }

        return result;
    }

    internal static string? ReadDirectText(global::System.Xml.XmlElement element, string[] names)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType == global::System.Xml.XmlNodeType.Element && DailyNameMatches((global::System.Xml.XmlElement)node, names))
            {
                string value = ((global::System.Xml.XmlElement)node).InnerText.Trim();
                if (value.Length > 0)
                {
                    return value;
                }
            }
        }

        return null;
    }

    internal static string? ReadFirstText(global::System.Xml.XmlElement element, string[] names)
    {
        global::System.Xml.XmlElement? found = FindFirstDescendant(element, names);
        if (found == null)
        {
            return null;
        }

        string value = found.InnerText.Trim();
        if (value.Length == 0)
        {
            return null;
        }

        return value;
    }

    internal static string[] ReadTextCollection(global::System.Xml.XmlElement element, string[] names)
    {
        global::System.Collections.Generic.List<global::System.Xml.XmlElement> elements = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        CollectDescendants(element, names, elements);
        global::System.Collections.Generic.List<string> values = new global::System.Collections.Generic.List<string>();
        int index;
        for (index = 0; index < elements.Count; index += 1)
        {
            string value = elements[index].InnerText.Trim();
            if (value.Length > 0)
            {
                values.Add(value);
            }
        }

        return values.ToArray();
    }

    internal static global::System.Xml.XmlElement? FindFirstDescendant(global::System.Xml.XmlElement element, string[] names)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType != global::System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            global::System.Xml.XmlElement child = (global::System.Xml.XmlElement)node;
            if (DailyNameMatches(child, names))
            {
                return child;
            }

            global::System.Xml.XmlElement? nested = FindFirstDescendant(child, names);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    internal static void CollectDescendants(global::System.Xml.XmlElement element, string[] names, global::System.Collections.Generic.List<global::System.Xml.XmlElement> destination)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType != global::System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            global::System.Xml.XmlElement child = (global::System.Xml.XmlElement)node;
            if (DailyNameMatches(child, names))
            {
                destination.Add(child);
            }
            else
            {
                CollectDescendants(child, names, destination);
            }
        }
    }

    internal static bool DailyNameMatches(global::System.Xml.XmlElement element, string[] names)
    {
        string normalized = NormalizeDailyName(element.LocalName);
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            if (normalized.Equals(NormalizeDailyName(names[index]), global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeDailyName(string value)
    {
        global::System.Text.StringBuilder result = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (global::System.Char.IsLetterOrDigit(character))
            {
                result.Append(global::System.Char.ToLowerInvariant(character));
            }
        }

        return result.ToString();
    }

    internal static int ReadDailyBoolean(string? text)
    {
        return TsdrXmlParser.ParseBoolean(text);
    }

    internal static void CaptureDailyRawValues(global::System.Xml.XmlElement element, string path, global::System.Collections.Generic.Dictionary<string, string> destination)
    {
        foreach (global::System.Xml.XmlAttribute attribute in element.Attributes)
        {
            if (!attribute.NamespaceURI.Equals("http://www.w3.org/2000/xmlns/", global::System.StringComparison.Ordinal))
            {
                destination[path + "/@" + attribute.LocalName] = attribute.Value;
            }
        }

        global::System.Collections.Generic.List<global::System.Xml.XmlElement> children = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType == global::System.Xml.XmlNodeType.Element)
            {
                children.Add((global::System.Xml.XmlElement)node);
            }
        }

        if (children.Count == 0)
        {
            destination[path] = element.InnerText;
            return;
        }

        int index;
        for (index = 0; index < children.Count; index += 1)
        {
            global::System.Xml.XmlElement child = children[index];
            int occurrence = 1;
            int total = 0;
            int siblingIndex;
            for (siblingIndex = 0; siblingIndex < children.Count; siblingIndex += 1)
            {
                if (children[siblingIndex].LocalName.Equals(child.LocalName, global::System.StringComparison.Ordinal))
                {
                    total += 1;
                    if (siblingIndex < index)
                    {
                        occurrence += 1;
                    }
                }
            }

            string childPath = path + "/" + child.LocalName;
            if (total > 1)
            {
                childPath += "[" + occurrence.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            }

            CaptureDailyRawValues(child, childPath, destination);
        }
    }

    internal static ParseException DailyFailure(string message, string path, XmlRecord record)
    {
        return new ParseException(message, DocumentFormat.Xml, "trademark daily XML", path, 0L, false, record.StartLineNumber, record.HasLineInfo, null);
    }
}
