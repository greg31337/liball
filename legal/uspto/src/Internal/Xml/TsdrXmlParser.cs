namespace uspto;

internal class TsdrXmlElement
{
    internal string Name = string.Empty;
    internal string Text = string.Empty;
    internal global::System.Collections.Generic.List<TsdrXmlElement> Children = new global::System.Collections.Generic.List<TsdrXmlElement>();
}

internal static class TsdrXmlParser
{
    internal static TrademarkCaseStatus ParseStatus(string xml, string requestedIdentifier, TrademarkIdentifierResult expectedIdentifier, HttpResponseMetadata metadata, string? rawResponse, UsptoLimits limits)
    {
        if (xml == null)
        {
            throw new ValidationException("A TSDR XML response is required.", "xml", "REQUIRED", null);
        }

        if (expectedIdentifier == null || !expectedIdentifier.IsValid)
        {
            throw new ValidationException("A valid expected trademark identifier is required.", "expectedIdentifier", "INVALID_IDENTIFIER", null);
        }

        if (metadata == null)
        {
            throw new ValidationException("TSDR response metadata is required.", "metadata", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("TSDR XML limits are required.", "limits", "REQUIRED", null);
        }

        byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes(xml);
        using (global::System.IO.MemoryStream input = new global::System.IO.MemoryStream(bytes, false))
        {
            TsdrXmlElement root = ReadTree(input, limits);
            TsdrXmlElement? trademark = FindFirst(root, new string[] { "Trademark" });
            if (trademark == null)
            {
                trademark = root;
            }

            TsdrXmlElement? status = FindFirst(trademark, new string[] { "Status" });
            if (status == null)
            {
                status = trademark;
            }

            TrademarkCaseStatus result = TsdrJsonParser.CreateResult(requestedIdentifier, expectedIdentifier, metadata, DocumentFormat.Xml, rawResponse);
            ApplyIdentity(result, status);
            ApplyStatus(result.Status, status);
            ApplyMark(result.Mark, trademark, status);
            ApplyBasis(result.FilingBasis, status);
            ApplyOwners(result, trademark);
            ApplyAttorneys(result, trademark);
            ApplyCorrespondence(result.Correspondence, status);
            result.ProsecutionEvents = ParseEvents(trademark);
            result.GoodsAndServices = ParseGoods(trademark);
            result.ForeignRegistrations = ParseForeign(trademark);
            ApplyMadrid(result, trademark);
            result.SearchIdentifier = ReadText(root, new string[] { "SearchId", "SearchIdentifier", "RequestIdentifier" });
            CaptureUnknownElements(trademark, new string[] { "Status", "Parties", "Attorneys", "AttorneyList", "ProsecutionHistory", "ProsecutionHistoryEvents", "GsList", "GoodsAndServices", "ForeignInfoList", "ForeignInformation", "IntlRegistrationList", "InternationalRegistrations", "RelationshipBundleList", "Relationships", "UsReference", "MadridData", "InternationalData", "SoundFile", "MultimediaFile", "MultimediaFiles" }, result.UnknownFields);
            result.IsComplete = true;
            return result;
        }
    }

    internal static TsdrXmlElement ReadTree(global::System.IO.Stream input, UsptoLimits limits)
    {
        SecureXmlReader reader = new SecureXmlReader(input, "TSDR case status", limits, false);
        global::System.Collections.Generic.List<TsdrXmlElement> stack = new global::System.Collections.Generic.List<TsdrXmlElement>();
        TsdrXmlElement? root = null;
        while (reader.Read())
        {
            if (reader.Current.NodeType == global::System.Xml.XmlNodeType.Element)
            {
                TsdrXmlElement element = new TsdrXmlElement();
                element.Name = reader.Current.LocalName;
                if (stack.Count > 0)
                {
                    stack[stack.Count - 1].Children.Add(element);
                }
                else if (root == null)
                {
                    root = element;
                }

                if (!reader.Current.IsEmptyElement)
                {
                    stack.Add(element);
                }
            }
            else if (IsTextNode(reader.Current.NodeType) && stack.Count > 0)
            {
                stack[stack.Count - 1].Text += reader.Current.Value;
            }
            else if (reader.Current.NodeType == global::System.Xml.XmlNodeType.EndElement && stack.Count > 0)
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }

        reader.Close();
        if (root == null)
        {
            throw ParseFailure("The TSDR XML response does not contain a document root.", "/");
        }

        return root;
    }

    internal static bool IsTextNode(global::System.Xml.XmlNodeType nodeType)
    {
        return nodeType == global::System.Xml.XmlNodeType.Text || nodeType == global::System.Xml.XmlNodeType.CDATA || nodeType == global::System.Xml.XmlNodeType.SignificantWhitespace || nodeType == global::System.Xml.XmlNodeType.Whitespace;
    }

    internal static void ApplyIdentity(TrademarkCaseStatus result, TsdrXmlElement status)
    {
        string? serial = ReadText(status, new string[] { "SerialNumber", "SerialNum" });
        string? registration = ReadText(status, new string[] { "USRegistrationNumber", "RegistrationNumber", "RegistrationNum" });
        if (!global::System.String.IsNullOrWhiteSpace(serial))
        {
            result.SerialNumber = serial;
        }

        if (!global::System.String.IsNullOrWhiteSpace(registration))
        {
            result.RegistrationNumber = registration;
        }
    }

    internal static void ApplyStatus(TrademarkStatus result, TsdrXmlElement status)
    {
        ApplyStatusInteger(result, ReadText(status, new string[] { "Status", "StatusCode" }), 1);
        result.CodeText = ReadText(status, new string[] { "StatusStr", "StatusCodeText" });
        result.ExternalDescription = ReadText(status, new string[] { "ExtStatusDesc", "StatusDescription", "ExternalDescription" });
        result.InternalDescription = ReadText(status, new string[] { "IntStatusDesc", "InternalDescription" });
        ApplyStatusInteger(result, ReadText(status, new string[] { "Tm5Status", "Tm5StatusCode" }), 2);
        result.Tm5Description = ReadText(status, new string[] { "Tm5StatusDesc", "Tm5Description" });
        result.Tm5Definition = ReadText(status, new string[] { "Tm5StatusDef", "Tm5Definition" });
        TsdrJsonParser.ApplyStatusDate(result, ParseDate(ReadText(status, new string[] { "StatusDate" })), 1);
        TsdrJsonParser.ApplyStatusDate(result, ParseDate(ReadText(status, new string[] { "FilingDate", "ApplicationDate" })), 2);
        TsdrJsonParser.ApplyStatusDate(result, ParseDate(ReadText(status, new string[] { "USRegistrationDate", "RegistrationDate" })), 3);
        TsdrJsonParser.ApplyStatusDate(result, ParseDate(ReadText(status, new string[] { "DateAbandoned", "AbandonmentDate" })), 4);
        TsdrJsonParser.ApplyStatusDate(result, ParseDate(ReadText(status, new string[] { "DateCancelled", "CancellationDate" })), 5);
        TsdrJsonParser.ApplyStatusDate(result, ParseDate(ReadText(status, new string[] { "DateRenewal", "RenewalDate" })), 6);
        result.RegisterCategory = ReadText(status, new string[] { "RegisterCategory", "Register" });
        result.LawOfficeCode = ReadText(status, new string[] { "LawOffAsgnCd", "LawOfficeCode", "LawOffAssigned" });
        result.CurrentLocationCode = ReadText(status, new string[] { "CurrLocationCd", "CurrentLocationCode" });
        result.CurrentLocationDescription = ReadText(status, new string[] { "CurrentLoc", "CurrentLocationDescription" });
        ApplyStatusInteger(result, ReadText(status, new string[] { "ClsCtActive", "ActiveClassCount" }), 3);
        ApplyStatusInteger(result, ReadText(status, new string[] { "ClsTotal", "TotalClassCount" }), 4);
    }

    internal static void ApplyMark(TrademarkMark result, TsdrXmlElement trademark, TsdrXmlElement status)
    {
        result.VerbalElementText = ReadText(status, new string[] { "MarkElement", "MarkVerbalElementText", "WordMark", "VerbalElementText" });
        result.Type = ReadText(status, new string[] { "MarkType" });
        result.DrawingCode = ReadText(status, new string[] { "MarkDrawingCd", "DrawingCode" });
        result.DrawingDescription = ReadText(status, new string[] { "MarkDrawDesc", "DrawingDescription" });
        result.FeatureCode = ReadText(status, new string[] { "MarkFeatureCode", "FeatureCode" });
        result.FeatureDescription = ReadText(status, new string[] { "MarkFeatureDescription", "FeatureDescription" });
        result.ColorClaimText = ReadText(status, new string[] { "ColorClaimed", "ColorClaimText" });
        result.ColorLocationText = ReadText(status, new string[] { "ColorLocation", "ColorLocationText" });
        result.Description = ReadText(status, new string[] { "DescOfMark", "MarkDescription", "Description" });
        result.DisclaimerText = ReadText(status, new string[] { "Disclaimer", "DisclaimerText" });
        result.PseudoMarkText = ReadText(status, new string[] { "PseudoMark", "PseudoMarkText" });
        result.TranslationText = ReadText(status, new string[] { "Translation", "TranslationText" });
        result.TransliterationText = ReadText(status, new string[] { "Transliteration", "TransliterationText" });
        result.NamePortraitConsentText = ReadText(status, new string[] { "NamePortrait", "NamePortraitConsentText" });
        result.LiningStipplingText = ReadText(status, new string[] { "LineStippling", "LiningStipplingText" });
        ApplyMarkBoolean(result, ReadText(status, new string[] { "StandardChar", "StandardCharacter" }), 1);
        ApplyMarkBoolean(result, ReadText(status, new string[] { "ColorDrawingCurr", "ColorDrawing" }), 2);
        result.DesignSearchClassifications = ParseDesignSearch(status);
        result.SoundFile = ReadText(trademark, new string[] { "SoundFile" });
        result.MultimediaFiles = ReadTextValues(trademark, new string[] { "MultimediaFile", "MultimediaFiles" });
    }

    internal static TrademarkDesignSearchClassification[] ParseDesignSearch(TsdrXmlElement status)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        Collect(status, new string[] { "DesignSearch" }, elements);
        global::System.Collections.Generic.List<TrademarkDesignSearchClassification> results = new global::System.Collections.Generic.List<TrademarkDesignSearchClassification>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkDesignSearchClassification value = new TrademarkDesignSearchClassification();
            value.Code = ReadText(element, new string[] { "Code" });
            value.Descriptions = ReadTextValues(element, new string[] { "Description", "Descriptions" });
            results.Add(value);
        }

        return results.ToArray();
    }

    internal static void ApplyBasis(TrademarkFilingBasis result, TsdrXmlElement source)
    {
        ApplyBasisBoolean(result, ReadText(source, new string[] { "FiledUse", "UseCurr", "Flag1a", "Section1A" }), 1);
        ApplyBasisBoolean(result, ReadText(source, new string[] { "FiledItu", "ItuCurr", "Flag1b", "Section1B" }), 2);
        ApplyBasisBoolean(result, ReadText(source, new string[] { "Filed44d", "Sect44dCurr", "Flag44d", "Section44D" }), 3);
        ApplyBasisBoolean(result, ReadText(source, new string[] { "Filed44e", "Sect44eCurr", "Flag44e", "Section44E" }), 4);
        ApplyBasisBoolean(result, ReadText(source, new string[] { "Filed66a", "Sect66aCurr", "Section66A" }), 5);
    }

    internal static void ApplyOwners(TrademarkCaseStatus result, TsdrXmlElement trademark)
    {
        TsdrXmlElement? parties = FindFirst(trademark, new string[] { "Parties" });
        if (parties == null)
        {
            return;
        }

        TsdrXmlElement? current = FindFirst(parties, new string[] { "CurrentOwners" });
        TsdrXmlElement? prior = FindFirst(parties, new string[] { "PriorOwnerList", "PriorOwners" });
        result.CurrentOwners = ParseOwners(current, true);
        result.PriorOwners = ParseOwners(prior, false);
    }

    internal static TrademarkOwner[] ParseOwners(TsdrXmlElement? container, bool isCurrent)
    {
        if (container == null)
        {
            return new TrademarkOwner[0];
        }

        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        Collect(container, new string[] { "Party", "Owner" }, elements);
        global::System.Collections.Generic.List<TrademarkOwner> results = new global::System.Collections.Generic.List<TrademarkOwner>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkOwner value = new TrademarkOwner();
            ApplyOwnerInteger(value, ReadText(element, new string[] { "EntityNum", "SequenceNumber" }), 1);
            ApplyOwnerInteger(value, ReadText(element, new string[] { "PartyType" }), 2);
            value.PartyTypeDescription = ReadText(element, new string[] { "PartyTypeDescription", "PartyTypeString" });
            value.Name = ReadText(element, new string[] { "Name", "LegalEntityName" });
            value.EntityTypeCode = ReadText(element, new string[] { "Code", "EntityTypeCode" });
            value.EntityTypeDescription = ReadText(element, new string[] { "EntityTypeDescription" });
            value.AlternateNameText = ReadText(element, new string[] { "DbaAkaFormerly", "AlternateNameText" });
            value.CompositionText = ReadText(element, new string[] { "ComposedOf", "OwnershipComposition" });
            value.AssignmentReelFrame = ReadText(element, new string[] { "ReelFrame", "Assignment" });
            value.CitizenshipCountryCode = ReadText(element, new string[] { "CitizenshipCountryCode", "CitizenshipCode" });
            value.CitizenshipCountryName = ReadText(element, new string[] { "CitizenshipCountryName", "CitizenshipCountry" });
            value.EmailAddresses = ReadTextValues(element, new string[] { "Address", "EmailAddress" });
            value.Address = ParseAddress(element);
            value.IsCurrent = isCurrent;
            value.HasIsCurrent = true;
            results.Add(value);
        }

        return results.ToArray();
    }

    internal static void ApplyAttorneys(TrademarkCaseStatus result, TsdrXmlElement trademark)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        Collect(trademark, new string[] { "Attorney", "RecordAttorney" }, elements);
        global::System.Collections.Generic.List<TrademarkAttorney> results = new global::System.Collections.Generic.List<TrademarkAttorney>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkAttorney value = new TrademarkAttorney();
            value.EmployeeNumber = ReadText(element, new string[] { "EmployeeNumber", "Number" });
            value.FirstName = ReadText(element, new string[] { "FirstName" });
            value.MiddleName = ReadText(element, new string[] { "MiddleName" });
            value.LastName = ReadText(element, new string[] { "LastName" });
            value.NameSuffix = ReadText(element, new string[] { "NameSuffix", "Suffix" });
            value.Name = ReadText(element, new string[] { "Name", "FullName" });
            value.Organization = ReadText(element, new string[] { "Organization", "FirmName" });
            value.DocketNumber = ReadText(element, new string[] { "DocketNumber", "AttrnyDktNumber" });
            value.EmailAddresses = ReadTextValues(element, new string[] { "EmailAddress", "EmailAddresses" });
            value.PhoneNumbers = ReadTextValues(element, new string[] { "PhoneNumber", "PhoneNumbers" });
            value.Address = ParseAddress(element);
            results.Add(value);
        }

        result.Attorneys = results.ToArray();
    }

    internal static void ApplyCorrespondence(TrademarkCorrespondence result, TsdrXmlElement status)
    {
        TsdrXmlElement? element = FindFirst(status, new string[] { "Correspondence" });
        if (element == null)
        {
            return;
        }

        result.Name = ReadText(element, new string[] { "Name" });
        result.Organization = ReadText(element, new string[] { "Organization" });
        result.EmailAddresses = ReadTextValues(element, new string[] { "EmailAddress", "EmailAddresses" });
        result.PhoneNumbers = ReadTextValues(element, new string[] { "PhoneNumber", "PhoneNumbers" });
        result.Address = ParseAddress(element);
    }

    internal static TrademarkProsecutionEvent[] ParseEvents(TsdrXmlElement trademark)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        TsdrXmlElement? history = FindFirst(trademark, new string[] { "ProsecutionHistory" });
        if (history != null)
        {
            Collect(history, new string[] { "Prosecution", "ProsecutionHistoryEvent" }, elements);
        }

        global::System.Collections.Generic.List<TrademarkProsecutionEvent> results = new global::System.Collections.Generic.List<TrademarkProsecutionEvent>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkProsecutionEvent value = new TrademarkProsecutionEvent();
            value.Code = ReadText(element, new string[] { "EntryCode", "Code", "EventActionCode" });
            value.Type = ReadText(element, new string[] { "EntryType", "Type" });
            value.Description = ReadText(element, new string[] { "EntryDesc", "Description", "EventDesc" });
            value.ProceedingNumber = ReadText(element, new string[] { "ProceedingNum", "ProceedingNumber" });
            value.DocumentIdentifier = ReadText(element, new string[] { "DocId", "DocumentIdentifier" });
            value.DocumentUri = TsdrJsonParser.ParseUri(ReadText(element, new string[] { "DocumentUrl", "DocumentUri" }));
            ApplyEventInteger(value, ReadText(element, new string[] { "EntryNumber" }), 1);
            ApplyEventInteger(value, ReadText(element, new string[] { "SequenceNum", "SequenceNumber" }), 2);
            ApplyEventBoolean(value, ReadText(element, new string[] { "Confidential" }));
            TsdrJsonParser.ApplyEventDate(value, ParseDate(ReadText(element, new string[] { "EntryDate", "Date", "EventDate", "EventDateTime" })), 1);
            TsdrJsonParser.ApplyEventDate(value, ParseDate(ReadText(element, new string[] { "DueDate" })), 2);
            TsdrJsonParser.ApplyEventDate(value, ParseDate(ReadText(element, new string[] { "RecordalDate" })), 3);
            results.Add(value);
        }

        return results.ToArray();
    }

    internal static TrademarkGoodsAndService[] ParseGoods(TsdrXmlElement trademark)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        Collect(trademark, new string[] { "GoodsAndService" }, elements);
        global::System.Collections.Generic.List<TrademarkGoodsAndService> results = new global::System.Collections.Generic.List<TrademarkGoodsAndService>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkGoodsAndService value = new TrademarkGoodsAndService();
            value.SerialNumber = ReadText(element, new string[] { "SerialNumber" });
            value.PrimeClassCode = ReadText(element, new string[] { "PrimeClassCode" });
            value.Description = ReadText(element, new string[] { "Description" });
            value.StatusCode = ReadText(element, new string[] { "StatusCode" });
            value.StatusDescription = ReadText(element, new string[] { "StatusDescription" });
            TsdrJsonParser.ApplyGoodsDate(value, ParseDate(ReadText(element, new string[] { "StatusDate" })), 1);
            TsdrJsonParser.ApplyGoodsDate(value, ParseDate(ReadText(element, new string[] { "FirstUseDate" })), 2);
            TsdrJsonParser.ApplyGoodsDate(value, ParseDate(ReadText(element, new string[] { "FirstUseInCommerceDate" })), 3);
            value.FirstUseDateDescription = ReadText(element, new string[] { "FirstUseDateDescription" });
            value.FirstUseInCommerceDateDescription = ReadText(element, new string[] { "FirstUseInCommerceDateDescription" });
            value.PseudoClassCodes = ReadTextValues(element, new string[] { "PseudoClass", "PseudoClasses" });
            value.Classes = ParseClasses(element);
            TsdrXmlElement? basis = FindFirst(element, new string[] { "ClassBasis", "FilingBasis" });
            if (basis != null)
            {
                ApplyBasis(value.FilingBasis, basis);
            }

            results.Add(value);
        }

        return results.ToArray();
    }

    internal static TrademarkClass[] ParseClasses(TsdrXmlElement goods)
    {
        global::System.Collections.Generic.List<TrademarkClass> results = new global::System.Collections.Generic.List<TrademarkClass>();
        AddClasses(results, FindFirst(goods, new string[] { "InternationalClasses" }), "international");
        AddClasses(results, FindFirst(goods, new string[] { "USClasses" }), "us");
        return results.ToArray();
    }

    internal static void AddClasses(global::System.Collections.Generic.List<TrademarkClass> results, TsdrXmlElement? container, string category)
    {
        if (container == null)
        {
            return;
        }

        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        Collect(container, new string[] { "TmClass", "Class" }, elements);
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkClass value = new TrademarkClass();
            value.Category = category;
            value.Code = ReadText(element, new string[] { "Code", "ClassNumber" });
            value.Description = ReadText(element, new string[] { "Description", "ClassTitleText" });
            ApplyClassBoolean(value, ReadText(element, new string[] { "IsPrime", "Prime" }));
            results.Add(value);
        }
    }

    internal static TrademarkForeignRegistration[] ParseForeign(TsdrXmlElement trademark)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        Collect(trademark, new string[] { "ForeignInformation" }, elements);
        global::System.Collections.Generic.List<TrademarkForeignRegistration> results = new global::System.Collections.Generic.List<TrademarkForeignRegistration>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkForeignRegistration value = new TrademarkForeignRegistration();
            value.ApplicationNumber = ReadText(element, new string[] { "FrgnApplNum", "ApplicationNumber" });
            value.RegistrationNumber = ReadText(element, new string[] { "FrgnRegNum", "RegistrationNumber" });
            value.CountryCode = ReadText(element, new string[] { "FrgnCountryCd", "CountryCode" });
            value.CountryName = ReadText(element, new string[] { "FrgnCountryName", "CountryName" });
            ApplyForeignBoolean(value, ReadText(element, new string[] { "IsPriorityClaimed", "PriorityClaimed" }));
            TsdrJsonParser.ApplyForeignDate(value, ParseDate(ReadText(element, new string[] { "FrgnApplFilingDt", "FilingDate" })), 1);
            TsdrJsonParser.ApplyForeignDate(value, ParseDate(ReadText(element, new string[] { "FrgnRegDt", "RegistrationDate" })), 2);
            TsdrJsonParser.ApplyForeignDate(value, ParseDate(ReadText(element, new string[] { "FrgnExpirationDt", "ExpirationDate" })), 3);
            results.Add(value);
        }

        return results.ToArray();
    }

    internal static void ApplyMadrid(TrademarkCaseStatus result, TsdrXmlElement trademark)
    {
        ApplyInternationalIndicator(result, ReadText(trademark, new string[] { "InternationalData" }));
        result.Madrid.Registrations = ParseInternationalRegistrations(trademark);
        result.Madrid.Relationships = ParseRelationships(trademark);
        TsdrXmlElement? reference = FindFirst(trademark, new string[] { "UsReference", "MadridData" });
        if (reference == null)
        {
            return;
        }

        result.Madrid.ReferenceNumber = ReadText(reference, new string[] { "ReferenceNum", "ReferenceNumber" });
        result.Madrid.InternationalRegistrationNumber = ReadText(reference, new string[] { "IntlRegistrationNum", "InternationalRegistrationNumber" });
        if (result.Madrid.ReferenceNumber != null)
        {
            result.ReferenceNumber = result.Madrid.ReferenceNumber;
        }

        result.InternationalRegistrationNumber = result.Madrid.InternationalRegistrationNumber;
        ApplyMadridInteger(result.Madrid, ReadText(reference, new string[] { "StatusCd", "StatusCode" }));
        result.Madrid.StatusCodeText = ReadText(reference, new string[] { "StatusCdStr", "StatusCodeText" });
        result.Madrid.StatusDescription = ReadText(reference, new string[] { "StatusDesc", "StatusDescription" });
        TsdrJsonParser.ApplyMadridDate(result.Madrid, ParseDate(ReadText(reference, new string[] { "OriginalFilingDate" })), 1);
        TsdrJsonParser.ApplyMadridDate(result.Madrid, ParseDate(ReadText(reference, new string[] { "StatusDate" })), 2);
        TsdrJsonParser.ApplyMadridDate(result.Madrid, ParseDate(ReadText(reference, new string[] { "ReplyByDate" })), 3);
        TsdrJsonParser.ApplyMadridDate(result.Madrid, ParseDate(ReadText(reference, new string[] { "RenewalDate" })), 4);
        result.Madrid.RelatedSerialNumbers = ReadTextValues(reference, new string[] { "RelatedSerialNumber", "RelatedSerialNumbrs" });
        result.Madrid.RelatedInternationalRegistrationNumbers = ReadTextValues(reference, new string[] { "RelatedInternationalRegistrationNumber", "RelatedIntlRegNumbrs" });
        result.Madrid.Events = ParseMadridEvents(reference);
    }

    internal static TrademarkInternationalRegistration[] ParseInternationalRegistrations(TsdrXmlElement trademark)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        Collect(trademark, new string[] { "InternationalRegistration" }, elements);
        global::System.Collections.Generic.List<TrademarkInternationalRegistration> results = new global::System.Collections.Generic.List<TrademarkInternationalRegistration>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkInternationalRegistration value = new TrademarkInternationalRegistration();
            value.RegistrationNumber = ReadText(element, new string[] { "RegistrationNum", "RegistrationNumber" });
            value.SerialNumber = ReadText(element, new string[] { "SerialNumber" });
            ApplyInternationalInteger(value, ReadText(element, new string[] { "StatusCd", "StatusCode" }));
            value.StatusDescription = ReadText(element, new string[] { "RegStatuDesc", "StatusDescription" });
            ApplyInternationalBoolean(value, ReadText(element, new string[] { "PriorityClaimed" }), 1);
            ApplyInternationalBoolean(value, ReadText(element, new string[] { "FirstRefusal" }), 2);
            TsdrJsonParser.ApplyInternationalDate(value, ParseDate(ReadText(element, new string[] { "RegistrationDate" })), 1);
            TsdrJsonParser.ApplyInternationalDate(value, ParseDate(ReadText(element, new string[] { "RegStatusDt", "StatusDate" })), 2);
            TsdrJsonParser.ApplyInternationalDate(value, ParseDate(ReadText(element, new string[] { "NotificationOfDesignationDt", "DesignationNotificationDate" })), 3);
            TsdrJsonParser.ApplyInternationalDate(value, ParseDate(ReadText(element, new string[] { "AutomaticProtectionDt", "AutomaticProtectionDate" })), 4);
            TsdrJsonParser.ApplyInternationalDate(value, ParseDate(ReadText(element, new string[] { "RegistrationCancelledDt", "CancellationDate" })), 5);
            TsdrJsonParser.ApplyInternationalDate(value, ParseDate(ReadText(element, new string[] { "RenewalDt", "RenewalDate" })), 6);
            results.Add(value);
        }

        return results.ToArray();
    }

    internal static TrademarkRelationship[] ParseRelationships(TsdrXmlElement trademark)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        Collect(trademark, new string[] { "RelationshipBundle", "Relationship" }, elements);
        global::System.Collections.Generic.List<TrademarkRelationship> results = new global::System.Collections.Generic.List<TrademarkRelationship>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkRelationship value = new TrademarkRelationship();
            value.SerialNumber = ReadText(element, new string[] { "SerialNumber" });
            value.RegistrationNumber = ReadText(element, new string[] { "USRegistrationNumber", "RegistrationNumber" });
            value.ReferenceNumber = ReadText(element, new string[] { "ReferenceNumber" });
            value.InternationalRegistrationNumber = ReadText(element, new string[] { "InternationalRegistrationNumber" });
            results.Add(value);
        }

        return results.ToArray();
    }

    internal static TrademarkMadridEvent[] ParseMadridEvents(TsdrXmlElement reference)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        TsdrXmlElement? history = FindFirst(reference, new string[] { "MadridHistory", "Events" });
        if (history != null)
        {
            Collect(history, new string[] { "Event" }, elements);
        }

        global::System.Collections.Generic.List<TrademarkMadridEvent> results = new global::System.Collections.Generic.List<TrademarkMadridEvent>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkMadridEvent value = new TrademarkMadridEvent();
            value.SerialNumber = ReadText(element, new string[] { "EventSerialNum", "SerialNumber" });
            value.ActionCode = ReadText(element, new string[] { "EventActionCode", "ActionCode" });
            value.Description = ReadText(element, new string[] { "EventDesc", "Description" });
            value.DocumentIdentifier = ReadText(element, new string[] { "DocId", "DocumentIdentifier" });
            TsdrJsonParser.ApplyMadridEventDate(value, ParseDate(ReadText(element, new string[] { "EventDateTime", "EventDate" })), 1);
            TsdrJsonParser.ApplyMadridEventDate(value, ParseDate(ReadText(element, new string[] { "RecordalDate" })), 2);
            results.Add(value);
        }

        return results.ToArray();
    }

    internal static TrademarkAddress ParseAddress(TsdrXmlElement source)
    {
        TrademarkAddress result = new TrademarkAddress();
        result.Line1 = ReadText(source, new string[] { "Line1", "Address1" });
        result.Line2 = ReadText(source, new string[] { "Line2", "Address2" });
        result.City = ReadText(source, new string[] { "City" });
        result.StateOrProvince = ReadText(source, new string[] { "State", "StateOrProvince" });
        result.PostalCode = ReadText(source, new string[] { "PostalCode", "Zip" });
        result.CountryCode = ReadText(source, new string[] { "CountryCode" });
        result.CountryName = ReadText(source, new string[] { "CountryName" });
        return result;
    }

    internal static TsdrDateValue ParseDate(string? text)
    {
        TsdrDateValue result = new TsdrDateValue();
        result.Text = text;
        if (!global::System.String.IsNullOrWhiteSpace(text))
        {
            JsonDateParseResult parsed = JsonFieldReader.ParseFlexibleDate(text ?? string.Empty);
            if (parsed.IsValid)
            {
                result.Value = parsed.Value;
                result.HasValue = true;
            }
        }

        return result;
    }

    internal static void ApplyStatusInteger(TrademarkStatus target, string? text, int field)
    {
        int value = ParseNonnegativeInteger(text);
        if (value < 0)
        {
            return;
        }

        switch (field)
        {
            case 1:
                target.Code = value;
                target.HasCode = true;
                break;
            case 2:
                target.Tm5Code = value;
                target.HasTm5Code = true;
                break;
            case 3:
                target.ActiveClassCount = value;
                target.HasActiveClassCount = true;
                break;
            case 4:
                target.TotalClassCount = value;
                target.HasTotalClassCount = true;
                break;
        }
    }

    internal static void ApplyMarkBoolean(TrademarkMark target, string? text, int field)
    {
        int value = ParseBoolean(text);
        if (value < 0)
        {
            return;
        }

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

    internal static void ApplyBasisBoolean(TrademarkFilingBasis target, string? text, int field)
    {
        int value = ParseBoolean(text);
        if (value < 0)
        {
            return;
        }

        switch (field)
        {
            case 1:
                target.Section1A = value == 1;
                target.HasSection1A = true;
                break;
            case 2:
                target.Section1B = value == 1;
                target.HasSection1B = true;
                break;
            case 3:
                target.Section44D = value == 1;
                target.HasSection44D = true;
                break;
            case 4:
                target.Section44E = value == 1;
                target.HasSection44E = true;
                break;
            case 5:
                target.Section66A = value == 1;
                target.HasSection66A = true;
                break;
        }
    }

    internal static void ApplyOwnerInteger(TrademarkOwner target, string? text, int field)
    {
        int value = ParseNonnegativeInteger(text);
        if (value < 0)
        {
            return;
        }

        if (field == 1)
        {
            target.SequenceNumber = value;
            target.HasSequenceNumber = true;
        }
        else
        {
            target.PartyTypeCode = value;
            target.HasPartyTypeCode = true;
        }
    }

    internal static void ApplyEventInteger(TrademarkProsecutionEvent target, string? text, int field)
    {
        int value = ParseNonnegativeInteger(text);
        if (value < 0)
        {
            return;
        }

        if (field == 1)
        {
            target.EntryNumber = value;
            target.HasEntryNumber = true;
        }
        else
        {
            target.SequenceNumber = value;
            target.HasSequenceNumber = true;
        }
    }

    internal static void ApplyForeignBoolean(TrademarkForeignRegistration target, string? text)
    {
        int value = ParseBoolean(text);
        if (value >= 0)
        {
            target.IsPriorityClaimed = value == 1;
            target.HasIsPriorityClaimed = true;
        }
    }

    internal static void ApplyClassBoolean(TrademarkClass target, string? text)
    {
        int value = ParseBoolean(text);
        if (value >= 0)
        {
            target.IsPrime = value == 1;
            target.HasIsPrime = true;
        }
    }

    internal static void ApplyEventBoolean(TrademarkProsecutionEvent target, string? text)
    {
        int value = ParseBoolean(text);
        if (value >= 0)
        {
            target.IsConfidential = value == 1;
            target.HasIsConfidential = true;
        }
    }

    internal static void ApplyInternationalIndicator(TrademarkCaseStatus target, string? text)
    {
        int value = ParseBoolean(text);
        if (value >= 0)
        {
            target.IsInternationalData = value == 1;
            target.HasIsInternationalData = true;
        }
    }

    internal static void ApplyMadridInteger(TrademarkMadridData target, string? text)
    {
        int value = ParseNonnegativeInteger(text);
        if (value >= 0)
        {
            target.StatusCode = value;
            target.HasStatusCode = true;
        }
    }

    internal static void ApplyInternationalInteger(TrademarkInternationalRegistration target, string? text)
    {
        int value = ParseNonnegativeInteger(text);
        if (value >= 0)
        {
            target.StatusCode = value;
            target.HasStatusCode = true;
        }
    }

    internal static void ApplyInternationalBoolean(TrademarkInternationalRegistration target, string? text, int field)
    {
        int value = ParseBoolean(text);
        if (value < 0)
        {
            return;
        }

        if (field == 1)
        {
            target.IsPriorityClaimed = value == 1;
            target.HasIsPriorityClaimed = true;
        }
        else
        {
            target.FirstRefusal = value == 1;
            target.HasFirstRefusal = true;
        }
    }

    internal static int ParseNonnegativeInteger(string? text)
    {
        if (global::System.String.IsNullOrWhiteSpace(text))
        {
            return -1;
        }

        string value = text.Trim();
        if (!TrademarkIdentifierParser.ContainsOnlyTrademarkDigits(value) || value.Length > 9)
        {
            return -1;
        }

        return global::System.Convert.ToInt32(value, global::System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static int ParseBoolean(string? text)
    {
        if (global::System.String.IsNullOrWhiteSpace(text))
        {
            return -1;
        }

        string value = text.Trim();
        if (value.Equals("true", global::System.StringComparison.OrdinalIgnoreCase) || value.Equals("Y", global::System.StringComparison.OrdinalIgnoreCase) || value == "1")
        {
            return 1;
        }

        if (value.Equals("false", global::System.StringComparison.OrdinalIgnoreCase) || value.Equals("N", global::System.StringComparison.OrdinalIgnoreCase) || value == "0")
        {
            return 0;
        }

        return -1;
    }

    internal static string? ReadText(TsdrXmlElement source, string[] names)
    {
        TsdrXmlElement? element = FindFirst(source, names);
        if (element == null)
        {
            return null;
        }

        string value = element.Text.Trim();
        if (value.Length == 0)
        {
            return null;
        }

        return value;
    }

    internal static string[] ReadTextValues(TsdrXmlElement source, string[] names)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        Collect(source, names, elements);
        global::System.Collections.Generic.List<string> values = new global::System.Collections.Generic.List<string>();
        foreach (TsdrXmlElement element in elements)
        {
            string text = element.Text.Trim();
            if (text.Length > 0)
            {
                values.Add(text);
            }
        }

        return values.ToArray();
    }

    internal static TsdrXmlElement? FindFirst(TsdrXmlElement source, string[] names)
    {
        foreach (TsdrXmlElement child in source.Children)
        {
            if (NameMatches(child.Name, names))
            {
                return child;
            }

            TsdrXmlElement? nested = FindFirst(child, names);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    internal static void Collect(TsdrXmlElement source, string[] names, global::System.Collections.Generic.List<TsdrXmlElement> results)
    {
        foreach (TsdrXmlElement child in source.Children)
        {
            if (NameMatches(child.Name, names))
            {
                results.Add(child);
            }
            else
            {
                Collect(child, names, results);
            }
        }
    }

    internal static bool NameMatches(string name, string[] expected)
    {
        int index;
        for (index = 0; index < expected.Length; index += 1)
        {
            if (name.Equals(expected[index], global::System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static void CaptureUnknownElements(TsdrXmlElement source, string[] knownNames, global::System.Collections.Generic.Dictionary<string, string> destination)
    {
        foreach (TsdrXmlElement child in source.Children)
        {
            if (!NameMatches(child.Name, knownNames))
            {
                destination[child.Name] = SerializeUnknownElement(child);
            }
        }
    }

    internal static string SerializeUnknownElement(TsdrXmlElement element)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        AppendUnknownElement(builder, element);
        return builder.ToString();
    }

    internal static void AppendUnknownElement(global::System.Text.StringBuilder builder, TsdrXmlElement element)
    {
        builder.Append('<');
        builder.Append(element.Name);
        builder.Append('>');
        if (element.Text.Length != 0)
        {
            string text = element.Text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            builder.Append(text);
        }

        foreach (TsdrXmlElement child in element.Children)
        {
            AppendUnknownElement(builder, child);
        }

        builder.Append("</");
        builder.Append(element.Name);
        builder.Append('>');
    }

    internal static ParseException ParseFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Xml, "TSDR case status", path, 0L, false, 0, false, null);
    }
}
