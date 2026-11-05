namespace uspto;

internal class TsdrJsonElementValue
{
    internal bool IsPresent = false;
    internal global::System.Text.Json.JsonElement Element = default(global::System.Text.Json.JsonElement);
    internal string Path = "$";
}

internal class TsdrDateValue
{
    internal string? Text = null;
    internal global::System.DateTimeOffset Value = default(global::System.DateTimeOffset);
    internal bool HasValue = false;
}

internal static class TsdrJsonParser
{
    internal static TrademarkCaseStatus ParseStatus(string json, string requestedIdentifier, TrademarkIdentifierResult expectedIdentifier, HttpResponseMetadata metadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("A TSDR JSON response is required.", "json", "REQUIRED", null);
        }

        if (expectedIdentifier == null || !expectedIdentifier.IsValid)
        {
            throw new ValidationException("A valid expected trademark identifier is required.", "expectedIdentifier", "INVALID_IDENTIFIER", null);
        }

        if (metadata == null)
        {
            throw new ValidationException("TSDR response metadata is required.", "metadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            TsdrJsonElementValue trademarkValue = LocateTrademark(document.RootElement);
            if (!trademarkValue.IsPresent || trademarkValue.Element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ParseFailure("The TSDR JSON response does not contain a trademark case.", trademarkValue.Path);
            }

            TrademarkCaseStatus result = CreateResult(requestedIdentifier, expectedIdentifier, metadata, DocumentFormat.Json, rawResponse);
            global::System.Text.Json.JsonElement trademark = trademarkValue.Element;
            TsdrJsonElementValue statusValue = Find(trademark, new string[] { "status", "Status" }, trademarkValue.Path);
            global::System.Text.Json.JsonElement status = trademark;
            string statusPath = trademarkValue.Path;
            if (statusValue.IsPresent)
            {
                if (statusValue.Element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
                {
                    throw ParseFailure("The TSDR trademark status is invalid.", statusValue.Path);
                }

                status = statusValue.Element;
                statusPath = statusValue.Path;
            }

            ParseIdentity(result, status, statusPath);
            ParseStatusFields(result.Status, status, statusPath);
            ParseMark(result.Mark, trademark, status, statusPath);
            ParseFilingBasis(result.FilingBasis, status, statusPath);
            ParseParties(result, trademark, trademarkValue.Path);
            ParseAttorneys(result, trademark, trademarkValue.Path);
            ParseCorrespondence(result.Correspondence, status, statusPath);
            result.ProsecutionEvents = ParseProsecutionEvents(trademark, trademarkValue.Path);
            result.GoodsAndServices = ParseGoods(trademark, trademarkValue.Path);
            result.ForeignRegistrations = ParseForeignRegistrations(trademark, trademarkValue.Path);
            ParseMadrid(result, trademark, trademarkValue.Path);
            result.SearchIdentifier = ReadString(document.RootElement, new string[] { "searchId", "searchIdentifier", "requestIdentifier" }, "$", false);
            CaptureUnknown(trademark, new string[] { "status", "Status", "parties", "Parties", "attorneys", "attorneyList", "prosecutionHistory", "prosecutionHistoryEvents", "gsList", "goodsAndServices", "foreignInfoList", "foreignInformation", "intlRegistrationList", "internationalRegistrations", "relationshipBundleList", "relationships", "usReference", "madridData", "internationalData", "soundFile", "multimediaFiles" }, result.UnknownFields);
            result.IsComplete = true;
            return result;
        }
    }

    internal static TsdrJsonElementValue LocateTrademark(global::System.Text.Json.JsonElement root)
    {
        TsdrJsonElementValue result = new TsdrJsonElementValue();
        result.Path = "$";
        if (root.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            return result;
        }

        TsdrJsonElementValue value = Find(root, new string[] { "trademarks", "Trademarks", "trademark", "Trademark" }, "$");
        if (!value.IsPresent)
        {
            result.IsPresent = true;
            result.Element = root;
            return result;
        }

        if (value.Element.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            return value;
        }

        if (value.Element.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            int count = value.Element.GetArrayLength();
            if (count == 1)
            {
                result.IsPresent = true;
                result.Element = value.Element[0];
                result.Path = value.Path + "[0]";
            }

            return result;
        }

        return result;
    }

    internal static TrademarkCaseStatus CreateResult(string requestedIdentifier, TrademarkIdentifierResult expectedIdentifier, HttpResponseMetadata metadata, DocumentFormat format, string? rawResponse)
    {
        TrademarkCaseStatus result = new TrademarkCaseStatus();
        result.RequestedIdentifier = requestedIdentifier;
        result.CanonicalIdentifier = expectedIdentifier.CanonicalNumber;
        result.IdentifierKind = expectedIdentifier.Kind;
        ApplyExpectedIdentifier(result, expectedIdentifier);
        result.RawFormat = format;
        result.RawResponse = rawResponse;
        result.Provenance.Source = DataProvenance.Tsdr;
        result.Provenance.SourceUri = metadata.RequestUri;
        result.Provenance.DatasetIdentifier = "TSDR case status";
        result.Provenance.RequestIdentifier = metadata.RequestIdentifier;
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = true;
        ProvenanceLabeler.AddPatentField(result.Provenance, "*", result.Provenance);
        return result;
    }

    internal static void ApplyExpectedIdentifier(TrademarkCaseStatus result, TrademarkIdentifierResult identifier)
    {
        switch (identifier.Kind)
        {
            case TrademarkIdentifierKind.SerialNumber:
                result.SerialNumber = identifier.IdentifierValue;
                break;
            case TrademarkIdentifierKind.RegistrationNumber:
                result.RegistrationNumber = identifier.IdentifierValue;
                break;
            case TrademarkIdentifierKind.ReferenceNumber:
                result.ReferenceNumber = identifier.IdentifierValue;
                break;
        }
    }

    internal static void ParseIdentity(TrademarkCaseStatus result, global::System.Text.Json.JsonElement status, string path)
    {
        string? serialNumber = ReadString(status, new string[] { "serialNumber", "serialNum" }, path, false);
        string? registrationNumber = ReadString(status, new string[] { "usRegistrationNumber", "registrationNumber", "registrationNum" }, path, false);
        if (!global::System.String.IsNullOrWhiteSpace(serialNumber))
        {
            result.SerialNumber = serialNumber;
        }

        if (!global::System.String.IsNullOrWhiteSpace(registrationNumber))
        {
            result.RegistrationNumber = registrationNumber;
        }
    }

    internal static void ParseStatusFields(TrademarkStatus result, global::System.Text.Json.JsonElement status, string path)
    {
        ApplyInteger(status, new string[] { "status", "statusCode" }, path, result, 1);
        result.CodeText = ReadString(status, new string[] { "statusStr", "statusCodeText" }, path, false);
        result.ExternalDescription = ReadString(status, new string[] { "extStatusDesc", "statusDescription", "externalDescription" }, path, false);
        result.InternalDescription = ReadString(status, new string[] { "intStatusDesc", "internalDescription" }, path, false);
        ApplyInteger(status, new string[] { "tm5Status", "tm5StatusCode" }, path, result, 2);
        result.Tm5Description = ReadString(status, new string[] { "tm5StatusDesc", "tm5Description" }, path, false);
        result.Tm5Definition = ReadString(status, new string[] { "tm5StatusDef", "tm5Definition" }, path, false);
        ApplyStatusDate(result, ReadDate(status, new string[] { "statusDate" }, path), 1);
        ApplyStatusDate(result, ReadDate(status, new string[] { "filingDate", "applicationDate" }, path), 2);
        ApplyStatusDate(result, ReadDate(status, new string[] { "usRegistrationDate", "registrationDate" }, path), 3);
        ApplyStatusDate(result, ReadDate(status, new string[] { "dateAbandoned", "abandonmentDate" }, path), 4);
        ApplyStatusDate(result, ReadDate(status, new string[] { "dateCancelled", "cancellationDate" }, path), 5);
        ApplyStatusDate(result, ReadDate(status, new string[] { "dateRenewal", "renewalDate" }, path), 6);
        result.RegisterCategory = ReadString(status, new string[] { "registerCategory", "register" }, path, false);
        result.LawOfficeCode = ReadString(status, new string[] { "lawOffAsgnCd", "lawOfficeCode", "lawOffAssigned" }, path, false);
        result.CurrentLocationCode = ReadString(status, new string[] { "currLocationCd", "currentLocationCode" }, path, false);
        result.CurrentLocationDescription = ReadString(status, new string[] { "currentLoc", "currentLocationDescription" }, path, false);
        ApplyInteger(status, new string[] { "clsCtActive", "activeClassCount" }, path, result, 3);
        ApplyInteger(status, new string[] { "clsTotal", "totalClassCount" }, path, result, 4);
    }

    internal static void ParseMark(TrademarkMark result, global::System.Text.Json.JsonElement trademark, global::System.Text.Json.JsonElement status, string path)
    {
        result.VerbalElementText = ReadString(status, new string[] { "markElement", "markVerbalElementText", "wordMark", "verbalElementText" }, path, false);
        result.Type = ReadString(status, new string[] { "markType", "type" }, path, false);
        result.DrawingCode = ReadString(status, new string[] { "markDrawingCd", "drawingCode" }, path, false);
        result.DrawingDescription = ReadString(status, new string[] { "markDrawDesc", "drawingDescription" }, path, false);
        result.FeatureCode = ReadString(status, new string[] { "markFeatureCode", "featureCode" }, path, false);
        result.FeatureDescription = ReadString(status, new string[] { "markFeatureDescription", "featureDescription" }, path, false);
        ApplyMarkBoolean(status, new string[] { "standardChar", "standardCharacter" }, path, result, 1);
        ApplyMarkBoolean(status, new string[] { "colorDrawingCurr", "colorDrawing" }, path, result, 2);
        result.ColorClaimText = ReadString(status, new string[] { "colorClaimed", "colorClaimText" }, path, false);
        result.ColorLocationText = ReadString(status, new string[] { "colorLocation", "colorLocationText" }, path, false);
        result.Description = ReadString(status, new string[] { "descOfMark", "markDescription", "description" }, path, false);
        result.DisclaimerText = ReadString(status, new string[] { "disclaimer", "disclaimerText" }, path, false);
        result.PseudoMarkText = ReadString(status, new string[] { "pseudoMark", "pseudoMarkText" }, path, false);
        result.TranslationText = ReadString(status, new string[] { "translation", "translationText" }, path, false);
        result.TransliterationText = ReadString(status, new string[] { "transliteration", "transliterationText" }, path, false);
        result.NamePortraitConsentText = ReadString(status, new string[] { "namePortrait", "namePortraitConsentText" }, path, false);
        result.LiningStipplingText = ReadString(status, new string[] { "lineStippling", "liningStipplingText" }, path, false);
        result.DesignSearchClassifications = ParseDesignSearch(status, path);
        result.SoundFile = ReadString(trademark, new string[] { "soundFile" }, "$", false);
        result.MultimediaFiles = ReadStringArray(trademark, new string[] { "multimediaFiles" }, "$", false);
    }

    internal static TrademarkDesignSearchClassification[] ParseDesignSearch(global::System.Text.Json.JsonElement status, string path)
    {
        TsdrJsonElementValue array = Find(status, new string[] { "designSearchList", "designSearchClassifications" }, path);
        if (!array.IsPresent)
        {
            return new TrademarkDesignSearchClassification[0];
        }

        RequireArray(array, "The TSDR design-search collection is invalid.");
        global::System.Collections.Generic.List<TrademarkDesignSearchClassification> results = new global::System.Collections.Generic.List<TrademarkDesignSearchClassification>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            RequireObject(item, array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", "The TSDR design-search entry is invalid.");
            TrademarkDesignSearchClassification value = new TrademarkDesignSearchClassification();
            value.Code = ReadString(item, new string[] { "code" }, array.Path, false);
            value.Descriptions = ReadStringArray(item, new string[] { "descriptions", "description" }, array.Path, true);
            results.Add(value);
            index += 1;
        }

        return results.ToArray();
    }

    internal static void ParseFilingBasis(TrademarkFilingBasis result, global::System.Text.Json.JsonElement source, string path)
    {
        ApplyBasisBoolean(source, new string[] { "filedUse", "useCurr", "flag1a", "section1A" }, path, result, 1);
        ApplyBasisBoolean(source, new string[] { "filedItu", "ituCurr", "flag1b", "section1B" }, path, result, 2);
        ApplyBasisBoolean(source, new string[] { "filed44d", "sect44dCurr", "flag44d", "section44D" }, path, result, 3);
        ApplyBasisBoolean(source, new string[] { "filed44e", "sect44eCurr", "flag44e", "section44E" }, path, result, 4);
        ApplyBasisBoolean(source, new string[] { "filed66a", "sect66aCurr", "section66A" }, path, result, 5);
    }

    internal static void ParseParties(TrademarkCaseStatus result, global::System.Text.Json.JsonElement trademark, string path)
    {
        TsdrJsonElementValue parties = Find(trademark, new string[] { "parties", "Parties" }, path);
        if (!parties.IsPresent)
        {
            return;
        }

        RequireObject(parties.Element, parties.Path, "The TSDR parties value is invalid.");
        result.CurrentOwners = ParseOwners(parties.Element, new string[] { "currentOwners" }, parties.Path, true);
        result.PriorOwners = ParseOwners(parties.Element, new string[] { "priorOwnerList", "priorOwners" }, parties.Path, false);
    }

    internal static TrademarkOwner[] ParseOwners(global::System.Text.Json.JsonElement source, string[] names, string path, bool isCurrent)
    {
        TsdrJsonElementValue array = Find(source, names, path);
        if (!array.IsPresent)
        {
            return new TrademarkOwner[0];
        }

        RequireArray(array, "The TSDR owner collection is invalid.");
        global::System.Collections.Generic.List<TrademarkOwner> results = new global::System.Collections.Generic.List<TrademarkOwner>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            string itemPath = array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            RequireObject(item, itemPath, "The TSDR owner entry is invalid.");
            TrademarkOwner owner = new TrademarkOwner();
            ApplyOwnerInteger(item, new string[] { "entityNum", "sequenceNumber" }, itemPath, owner, 1);
            ApplyOwnerInteger(item, new string[] { "partyType" }, itemPath, owner, 2);
            owner.PartyTypeDescription = ReadString(item, new string[] { "partyTypeDescription", "partyTypeString" }, itemPath, false);
            owner.Name = ReadString(item, new string[] { "name", "legalEntityName" }, itemPath, false);
            owner.EntityTypeCode = ReadNestedEntity(item, "entityType", "code", itemPath);
            owner.EntityTypeDescription = ReadNestedEntity(item, "entityType", "name", itemPath);
            owner.AlternateNameText = ReadString(item, new string[] { "dbaAkaFormerly", "alternateNameText" }, itemPath, false);
            owner.CompositionText = ReadString(item, new string[] { "composedOf", "ownershipComposition" }, itemPath, false);
            owner.AssignmentReelFrame = ReadString(item, new string[] { "reelFrame", "assignment" }, itemPath, false);
            owner.CitizenshipCountryCode = ReadString(item, new string[] { "citizenshipCountryCode", "citizenshipCode" }, itemPath, false);
            owner.CitizenshipCountryName = ReadString(item, new string[] { "citizenshipCountryName", "citizenshipCountry" }, itemPath, false);
            owner.EmailAddresses = ReadNestedStringArray(item, "emailList", "addresses", itemPath);
            owner.Address = ParseAddress(item, itemPath);
            owner.IsCurrent = isCurrent;
            owner.HasIsCurrent = true;
            results.Add(owner);
            index += 1;
        }

        return results.ToArray();
    }

    internal static void ParseAttorneys(TrademarkCaseStatus result, global::System.Text.Json.JsonElement trademark, string path)
    {
        TsdrJsonElementValue array = Find(trademark, new string[] { "attorneys", "attorneyList" }, path);
        if (!array.IsPresent)
        {
            return;
        }

        RequireArray(array, "The TSDR attorney collection is invalid.");
        global::System.Collections.Generic.List<TrademarkAttorney> values = new global::System.Collections.Generic.List<TrademarkAttorney>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            string itemPath = array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            RequireObject(item, itemPath, "The TSDR attorney entry is invalid.");
            TrademarkAttorney attorney = new TrademarkAttorney();
            attorney.EmployeeNumber = ReadString(item, new string[] { "employeeNumber", "number" }, itemPath, false);
            attorney.FirstName = ReadString(item, new string[] { "firstName" }, itemPath, false);
            attorney.MiddleName = ReadString(item, new string[] { "middleName" }, itemPath, false);
            attorney.LastName = ReadString(item, new string[] { "lastName" }, itemPath, false);
            attorney.NameSuffix = ReadString(item, new string[] { "nameSuffix", "suffix" }, itemPath, false);
            attorney.Name = ReadString(item, new string[] { "name", "fullName" }, itemPath, false);
            attorney.Organization = ReadString(item, new string[] { "organization", "firmName" }, itemPath, false);
            attorney.DocketNumber = ReadString(item, new string[] { "docketNumber", "attrnyDktNumber" }, itemPath, false);
            attorney.EmailAddresses = ReadStringArray(item, new string[] { "emailAddresses", "emails" }, itemPath, false);
            attorney.PhoneNumbers = ReadStringArray(item, new string[] { "phoneNumbers", "phones" }, itemPath, false);
            attorney.Address = ParseAddress(item, itemPath);
            values.Add(attorney);
            index += 1;
        }

        result.Attorneys = values.ToArray();
    }

    internal static void ParseCorrespondence(TrademarkCorrespondence result, global::System.Text.Json.JsonElement status, string path)
    {
        TsdrJsonElementValue value = Find(status, new string[] { "correspondence", "Correspondence" }, path);
        if (!value.IsPresent)
        {
            return;
        }

        RequireObject(value.Element, value.Path, "The TSDR correspondence value is invalid.");
        result.Name = ReadString(value.Element, new string[] { "name" }, value.Path, false);
        result.Organization = ReadString(value.Element, new string[] { "organization" }, value.Path, false);
        result.EmailAddresses = ReadStringArray(value.Element, new string[] { "emailAddresses", "emails" }, value.Path, false);
        result.PhoneNumbers = ReadStringArray(value.Element, new string[] { "phoneNumbers", "phones" }, value.Path, false);
        result.Address = ParseAddress(value.Element, value.Path);
    }

    internal static TrademarkProsecutionEvent[] ParseProsecutionEvents(global::System.Text.Json.JsonElement trademark, string path)
    {
        TsdrJsonElementValue array = Find(trademark, new string[] { "prosecutionHistory", "prosecutionHistoryEvents" }, path);
        if (!array.IsPresent)
        {
            return new TrademarkProsecutionEvent[0];
        }

        RequireArray(array, "The TSDR prosecution-history collection is invalid.");
        global::System.Collections.Generic.List<TrademarkProsecutionEvent> results = new global::System.Collections.Generic.List<TrademarkProsecutionEvent>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            string itemPath = array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            RequireObject(item, itemPath, "The TSDR prosecution-history entry is invalid.");
            TrademarkProsecutionEvent value = new TrademarkProsecutionEvent();
            value.Code = ReadString(item, new string[] { "entryCode", "code", "eventActionCode" }, itemPath, false);
            value.Type = ReadString(item, new string[] { "entryType", "type" }, itemPath, false);
            value.Description = ReadString(item, new string[] { "entryDesc", "description", "eventDesc" }, itemPath, false);
            value.ProceedingNumber = ReadString(item, new string[] { "proceedingNum", "proceedingNumber" }, itemPath, false);
            value.DocumentIdentifier = ReadString(item, new string[] { "docId", "documentIdentifier" }, itemPath, false);
            ApplyEventInteger(item, new string[] { "entryNumber" }, itemPath, value, 1);
            ApplyEventInteger(item, new string[] { "sequenceNum", "sequenceNumber" }, itemPath, value, 2);
            ApplyEventBoolean(item, new string[] { "confidential" }, itemPath, value);
            ApplyEventDate(value, ReadDate(item, new string[] { "entryDate", "date", "eventDate", "eventDateTime" }, itemPath), 1);
            ApplyEventDate(value, ReadDate(item, new string[] { "dueDate" }, itemPath), 2);
            ApplyEventDate(value, ReadDate(item, new string[] { "recordalDate" }, itemPath), 3);
            string? documentUri = ReadString(item, new string[] { "documentUrl", "documentUri" }, itemPath, false);
            value.DocumentUri = ParseUri(documentUri);
            results.Add(value);
            index += 1;
        }

        return results.ToArray();
    }

    internal static TrademarkGoodsAndService[] ParseGoods(global::System.Text.Json.JsonElement trademark, string path)
    {
        TsdrJsonElementValue array = Find(trademark, new string[] { "gsList", "goodsAndServices" }, path);
        if (!array.IsPresent)
        {
            return new TrademarkGoodsAndService[0];
        }

        RequireArray(array, "The TSDR goods-and-services collection is invalid.");
        global::System.Collections.Generic.List<TrademarkGoodsAndService> results = new global::System.Collections.Generic.List<TrademarkGoodsAndService>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            string itemPath = array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            RequireObject(item, itemPath, "The TSDR goods-and-services entry is invalid.");
            TrademarkGoodsAndService value = new TrademarkGoodsAndService();
            value.SerialNumber = ReadString(item, new string[] { "serialNumber" }, itemPath, false);
            value.PrimeClassCode = ReadString(item, new string[] { "primeClassCode" }, itemPath, false);
            value.Description = ReadString(item, new string[] { "description" }, itemPath, false);
            value.StatusCode = ReadString(item, new string[] { "statusCode" }, itemPath, false);
            value.StatusDescription = ReadString(item, new string[] { "statusDescription" }, itemPath, false);
            ApplyGoodsDate(value, ReadDate(item, new string[] { "statusDate" }, itemPath), 1);
            ApplyGoodsDate(value, ReadDate(item, new string[] { "firstUseDate" }, itemPath), 2);
            ApplyGoodsDate(value, ReadDate(item, new string[] { "firstUseInCommerceDate" }, itemPath), 3);
            value.FirstUseDateDescription = ReadString(item, new string[] { "firstUseDateDescription" }, itemPath, false);
            value.FirstUseInCommerceDateDescription = ReadString(item, new string[] { "firstUseInCommerceDateDescription" }, itemPath, false);
            value.PseudoClassCodes = ReadStringArray(item, new string[] { "pseudoClasses", "pseudoClassCodes" }, itemPath, false);
            value.Classes = ParseClasses(item, itemPath);
            TsdrJsonElementValue basis = Find(item, new string[] { "classBasis", "filingBasis" }, itemPath);
            if (basis.IsPresent)
            {
                RequireObject(basis.Element, basis.Path, "The TSDR class filing basis is invalid.");
                ParseFilingBasis(value.FilingBasis, basis.Element, basis.Path);
            }

            results.Add(value);
            index += 1;
        }

        return results.ToArray();
    }

    internal static TrademarkClass[] ParseClasses(global::System.Text.Json.JsonElement goods, string path)
    {
        global::System.Collections.Generic.List<TrademarkClass> results = new global::System.Collections.Generic.List<TrademarkClass>();
        AddClasses(results, goods, new string[] { "internationalClasses" }, path, "international");
        AddClasses(results, goods, new string[] { "usClasses" }, path, "us");
        return results.ToArray();
    }

    internal static void AddClasses(global::System.Collections.Generic.List<TrademarkClass> results, global::System.Text.Json.JsonElement goods, string[] names, string path, string category)
    {
        TsdrJsonElementValue array = Find(goods, names, path);
        if (!array.IsPresent)
        {
            return;
        }

        RequireArray(array, "The TSDR class collection is invalid.");
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            string itemPath = array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            TrademarkClass value = new TrademarkClass();
            value.Category = category;
            if (item.ValueKind == global::System.Text.Json.JsonValueKind.Object)
            {
                value.Code = ReadString(item, new string[] { "code", "classNumber" }, itemPath, false);
                value.Description = ReadString(item, new string[] { "description", "classTitleText" }, itemPath, false);
                ApplyClassBoolean(item, new string[] { "isPrime", "prime" }, itemPath, value);
            }
            else
            {
                value.Code = ScalarText(item, itemPath, false);
            }

            results.Add(value);
            index += 1;
        }
    }

    internal static TrademarkForeignRegistration[] ParseForeignRegistrations(global::System.Text.Json.JsonElement trademark, string path)
    {
        TsdrJsonElementValue array = Find(trademark, new string[] { "foreignInfoList", "foreignInformation" }, path);
        if (!array.IsPresent)
        {
            return new TrademarkForeignRegistration[0];
        }

        RequireArray(array, "The TSDR foreign-information collection is invalid.");
        global::System.Collections.Generic.List<TrademarkForeignRegistration> results = new global::System.Collections.Generic.List<TrademarkForeignRegistration>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            string itemPath = array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            RequireObject(item, itemPath, "The TSDR foreign-information entry is invalid.");
            TrademarkForeignRegistration value = new TrademarkForeignRegistration();
            value.ApplicationNumber = ReadString(item, new string[] { "frgnApplNum", "applicationNumber" }, itemPath, false);
            value.RegistrationNumber = ReadString(item, new string[] { "frgnRegNum", "registrationNumber" }, itemPath, false);
            value.CountryCode = ReadString(item, new string[] { "frgnCountryCd", "countryCode" }, itemPath, false);
            value.CountryName = ReadString(item, new string[] { "frgnCountryName", "countryName" }, itemPath, false);
            ApplyForeignBoolean(item, new string[] { "isPriorityClaimed", "priorityClaimed" }, itemPath, value);
            ApplyForeignDate(value, ReadDate(item, new string[] { "frgnApplFilingDt", "filingDate" }, itemPath), 1);
            ApplyForeignDate(value, ReadDate(item, new string[] { "frgnRegDt", "registrationDate" }, itemPath), 2);
            ApplyForeignDate(value, ReadDate(item, new string[] { "frgnExpirationDt", "expirationDate" }, itemPath), 3);
            results.Add(value);
            index += 1;
        }

        return results.ToArray();
    }

    internal static void ParseMadrid(TrademarkCaseStatus result, global::System.Text.Json.JsonElement trademark, string path)
    {
        ApplyCaseBoolean(trademark, new string[] { "internationalData" }, path, result);
        result.Madrid.Registrations = ParseInternationalRegistrations(trademark, path);
        result.Madrid.Relationships = ParseRelationships(trademark, path);
        TsdrJsonElementValue reference = Find(trademark, new string[] { "usReference", "madridData" }, path);
        if (!reference.IsPresent)
        {
            return;
        }

        RequireObject(reference.Element, reference.Path, "The TSDR Madrid reference is invalid.");
        result.Madrid.ReferenceNumber = ReadString(reference.Element, new string[] { "referenceNum", "referenceNumber" }, reference.Path, false);
        result.Madrid.InternationalRegistrationNumber = ReadString(reference.Element, new string[] { "intlRegistrationNum", "internationalRegistrationNumber" }, reference.Path, false);
        result.ReferenceNumber = result.Madrid.ReferenceNumber ?? result.ReferenceNumber;
        result.InternationalRegistrationNumber = result.Madrid.InternationalRegistrationNumber;
        ApplyMadridInteger(reference.Element, new string[] { "statusCd", "statusCode" }, reference.Path, result.Madrid);
        result.Madrid.StatusCodeText = ReadString(reference.Element, new string[] { "statusCdStr", "statusCodeText" }, reference.Path, false);
        result.Madrid.StatusDescription = ReadString(reference.Element, new string[] { "statusDesc", "statusDescription" }, reference.Path, false);
        ApplyMadridDate(result.Madrid, ReadDate(reference.Element, new string[] { "originalFilingDate" }, reference.Path), 1);
        ApplyMadridDate(result.Madrid, ReadDate(reference.Element, new string[] { "statusDate" }, reference.Path), 2);
        ApplyMadridDate(result.Madrid, ReadDate(reference.Element, new string[] { "replyByDate" }, reference.Path), 3);
        ApplyMadridDate(result.Madrid, ReadDate(reference.Element, new string[] { "renewalDate" }, reference.Path), 4);
        result.Madrid.RelatedSerialNumbers = ReadStringArray(reference.Element, new string[] { "relatedSerialNumbrs", "relatedSerialNumbers" }, reference.Path, false);
        result.Madrid.RelatedInternationalRegistrationNumbers = ReadStringArray(reference.Element, new string[] { "relatedIntlRegNumbrs", "relatedInternationalRegistrationNumbers" }, reference.Path, false);
        result.Madrid.Events = ParseMadridEvents(reference.Element, reference.Path);
    }

    internal static TrademarkInternationalRegistration[] ParseInternationalRegistrations(global::System.Text.Json.JsonElement trademark, string path)
    {
        TsdrJsonElementValue array = Find(trademark, new string[] { "intlRegistrationList", "internationalRegistrations" }, path);
        if (!array.IsPresent)
        {
            return new TrademarkInternationalRegistration[0];
        }

        RequireArray(array, "The TSDR international-registration collection is invalid.");
        global::System.Collections.Generic.List<TrademarkInternationalRegistration> results = new global::System.Collections.Generic.List<TrademarkInternationalRegistration>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            string itemPath = array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            RequireObject(item, itemPath, "The TSDR international-registration entry is invalid.");
            TrademarkInternationalRegistration value = new TrademarkInternationalRegistration();
            value.RegistrationNumber = ReadString(item, new string[] { "registrationNum", "registrationNumber" }, itemPath, false);
            value.SerialNumber = ReadString(item, new string[] { "serialNumber" }, itemPath, false);
            ApplyInternationalInteger(item, new string[] { "statusCd", "statusCode" }, itemPath, value);
            value.StatusDescription = ReadString(item, new string[] { "regStatuDesc", "statusDescription" }, itemPath, false);
            ApplyInternationalBoolean(item, new string[] { "priorityClaimed" }, itemPath, value, 1);
            ApplyInternationalBoolean(item, new string[] { "firstRefusal" }, itemPath, value, 2);
            ApplyInternationalDate(value, ReadDate(item, new string[] { "registrationDate" }, itemPath), 1);
            ApplyInternationalDate(value, ReadDate(item, new string[] { "regStatusDt", "statusDate" }, itemPath), 2);
            ApplyInternationalDate(value, ReadDate(item, new string[] { "notificationOfDesignationDt", "designationNotificationDate" }, itemPath), 3);
            ApplyInternationalDate(value, ReadDate(item, new string[] { "automaticProtectionDt", "automaticProtectionDate" }, itemPath), 4);
            ApplyInternationalDate(value, ReadDate(item, new string[] { "registrationCancelledDt", "cancellationDate" }, itemPath), 5);
            ApplyInternationalDate(value, ReadDate(item, new string[] { "renewalDt", "renewalDate" }, itemPath), 6);
            results.Add(value);
            index += 1;
        }

        return results.ToArray();
    }

    internal static TrademarkRelationship[] ParseRelationships(global::System.Text.Json.JsonElement trademark, string path)
    {
        TsdrJsonElementValue array = Find(trademark, new string[] { "relationshipBundleList", "relationships" }, path);
        if (!array.IsPresent)
        {
            return new TrademarkRelationship[0];
        }

        RequireArray(array, "The TSDR relationship collection is invalid.");
        global::System.Collections.Generic.List<TrademarkRelationship> results = new global::System.Collections.Generic.List<TrademarkRelationship>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            string itemPath = array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            RequireObject(item, itemPath, "The TSDR relationship entry is invalid.");
            TrademarkRelationship value = new TrademarkRelationship();
            value.SerialNumber = ReadString(item, new string[] { "serialNumber" }, itemPath, false);
            value.RegistrationNumber = ReadString(item, new string[] { "usRegistrationNumber", "registrationNumber" }, itemPath, false);
            value.ReferenceNumber = ReadString(item, new string[] { "referenceNumber" }, itemPath, false);
            value.InternationalRegistrationNumber = ReadString(item, new string[] { "internationalRegistrationNumber" }, itemPath, false);
            results.Add(value);
            index += 1;
        }

        return results.ToArray();
    }

    internal static TrademarkMadridEvent[] ParseMadridEvents(global::System.Text.Json.JsonElement reference, string path)
    {
        TsdrJsonElementValue array = Find(reference, new string[] { "madridHistory", "events" }, path);
        if (!array.IsPresent)
        {
            return new TrademarkMadridEvent[0];
        }

        RequireArray(array, "The TSDR Madrid-history collection is invalid.");
        global::System.Collections.Generic.List<TrademarkMadridEvent> results = new global::System.Collections.Generic.List<TrademarkMadridEvent>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in array.Element.EnumerateArray())
        {
            string itemPath = array.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            RequireObject(item, itemPath, "The TSDR Madrid-history entry is invalid.");
            TrademarkMadridEvent value = new TrademarkMadridEvent();
            value.SerialNumber = ReadString(item, new string[] { "eventSerialNum", "serialNumber" }, itemPath, false);
            value.ActionCode = ReadString(item, new string[] { "eventActionCode", "actionCode" }, itemPath, false);
            value.Description = ReadString(item, new string[] { "eventDesc", "description" }, itemPath, false);
            value.DocumentIdentifier = ReadString(item, new string[] { "docId", "documentIdentifier" }, itemPath, false);
            ApplyMadridEventDate(value, ReadDate(item, new string[] { "eventDateTime", "eventDate" }, itemPath), 1);
            ApplyMadridEventDate(value, ReadDate(item, new string[] { "recordalDate" }, itemPath), 2);
            results.Add(value);
            index += 1;
        }

        return results.ToArray();
    }

    internal static TrademarkAddress ParseAddress(global::System.Text.Json.JsonElement source, string path)
    {
        TrademarkAddress result = new TrademarkAddress();
        result.Line1 = ReadString(source, new string[] { "line1", "address1" }, path, false);
        result.Line2 = ReadString(source, new string[] { "line2", "address2" }, path, false);
        result.City = ReadString(source, new string[] { "city" }, path, false);
        result.StateOrProvince = ReadString(source, new string[] { "state", "stateOrProvince" }, path, false);
        result.PostalCode = ReadString(source, new string[] { "postalCode", "zip" }, path, false);
        result.CountryCode = ReadString(source, new string[] { "countryCode" }, path, false);
        result.CountryName = ReadString(source, new string[] { "countryName" }, path, false);
        return result;
    }

    internal static string? ReadNestedEntity(global::System.Text.Json.JsonElement source, string containerName, string memberName, string path)
    {
        TsdrJsonElementValue container = Find(source, new string[] { containerName }, path);
        if (!container.IsPresent || container.Element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(container.Element, new string[] { memberName }, container.Path, false);
    }

    internal static string[] ReadNestedStringArray(global::System.Text.Json.JsonElement source, string containerName, string memberName, string path)
    {
        TsdrJsonElementValue container = Find(source, new string[] { containerName }, path);
        if (!container.IsPresent || container.Element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            return new string[0];
        }

        return ReadStringArray(container.Element, new string[] { memberName }, container.Path, false);
    }

    internal static TsdrJsonElementValue Find(global::System.Text.Json.JsonElement source, string[] names, string path)
    {
        TsdrJsonElementValue result = new TsdrJsonElementValue();
        result.Path = path;
        if (source.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            return result;
        }

        foreach (global::System.Text.Json.JsonProperty property in source.EnumerateObject())
        {
            int index;
            for (index = 0; index < names.Length; index += 1)
            {
                if (property.Name.Equals(names[index], global::System.StringComparison.OrdinalIgnoreCase))
                {
                    result.IsPresent = true;
                    result.Element = property.Value;
                    result.Path = JsonFieldReader.AppendPropertyPath(path, property.Name);
                    return result;
                }
            }
        }

        return result;
    }

    internal static string? ReadString(global::System.Text.Json.JsonElement source, string[] names, string path, bool allowArray)
    {
        TsdrJsonElementValue value = Find(source, names, path);
        if (!value.IsPresent || value.Element.ValueKind == global::System.Text.Json.JsonValueKind.Null)
        {
            return null;
        }

        if (allowArray && value.Element.ValueKind == global::System.Text.Json.JsonValueKind.Array && value.Element.GetArrayLength() > 0)
        {
            return ScalarText(value.Element[0], value.Path + "[0]", false);
        }

        return ScalarText(value.Element, value.Path, false);
    }

    internal static string ScalarText(global::System.Text.Json.JsonElement value, string path, bool allowNull)
    {
        if (value.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        if (value.ValueKind == global::System.Text.Json.JsonValueKind.Number || value.ValueKind == global::System.Text.Json.JsonValueKind.True || value.ValueKind == global::System.Text.Json.JsonValueKind.False)
        {
            return value.GetRawText();
        }

        if (allowNull && value.ValueKind == global::System.Text.Json.JsonValueKind.Null)
        {
            return string.Empty;
        }

        throw ParseFailure("The TSDR field is not a scalar value.", path);
    }

    internal static string[] ReadStringArray(global::System.Text.Json.JsonElement source, string[] names, string path, bool allowScalar)
    {
        TsdrJsonElementValue value = Find(source, names, path);
        if (!value.IsPresent || value.Element.ValueKind == global::System.Text.Json.JsonValueKind.Null)
        {
            return new string[0];
        }

        if (allowScalar && value.Element.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            return new string[] { ScalarText(value.Element, value.Path, false) };
        }

        RequireArray(value, "The TSDR string collection is invalid.");
        global::System.Collections.Generic.List<string> results = new global::System.Collections.Generic.List<string>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in value.Element.EnumerateArray())
        {
            results.Add(ScalarText(item, value.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", false));
            index += 1;
        }

        return results.ToArray();
    }

    internal static TsdrDateValue ReadDate(global::System.Text.Json.JsonElement source, string[] names, string path)
    {
        TsdrDateValue result = new TsdrDateValue();
        result.Text = ReadString(source, names, path, false);
        if (!global::System.String.IsNullOrWhiteSpace(result.Text))
        {
            JsonDateParseResult parsed = JsonFieldReader.ParseFlexibleDate(result.Text ?? string.Empty);
            if (parsed.IsValid)
            {
                result.Value = parsed.Value;
                result.HasValue = true;
            }
        }

        return result;
    }

    internal static JsonFieldValue ReadInteger(global::System.Text.Json.JsonElement source, string[] names, string path)
    {
        TsdrJsonElementValue located = Find(source, names, path);
        JsonFieldValue result = new JsonFieldValue();
        result.Path = located.Path;
        if (!located.IsPresent || located.Element.ValueKind == global::System.Text.Json.JsonValueKind.Null)
        {
            result.IsValid = true;
            return result;
        }

        result.IsPresent = true;
        JsonIntegerParseResult parsed = JsonFieldReader.ParseIntegerElement(located.Element);
        if (parsed.IsValid && parsed.Value >= global::System.Int32.MinValue && parsed.Value <= global::System.Int32.MaxValue)
        {
            result.Int32Value = (int)parsed.Value;
            result.IsValid = true;
        }

        return result;
    }

    internal static JsonFieldValue ReadBoolean(global::System.Text.Json.JsonElement source, string[] names, string path)
    {
        TsdrJsonElementValue located = Find(source, names, path);
        JsonFieldValue result = new JsonFieldValue();
        result.Path = located.Path;
        if (!located.IsPresent || located.Element.ValueKind == global::System.Text.Json.JsonValueKind.Null)
        {
            result.IsValid = true;
            return result;
        }

        result.IsPresent = true;
        if (located.Element.ValueKind == global::System.Text.Json.JsonValueKind.True || located.Element.ValueKind == global::System.Text.Json.JsonValueKind.False)
        {
            result.BooleanValue = located.Element.ValueKind == global::System.Text.Json.JsonValueKind.True;
            result.IsValid = true;
            return result;
        }

        string text = ScalarText(located.Element, located.Path, false);
        if (text.Equals("true", global::System.StringComparison.OrdinalIgnoreCase) || text == "1" || text.Equals("Y", global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.BooleanValue = true;
            result.IsValid = true;
        }
        else if (text.Equals("false", global::System.StringComparison.OrdinalIgnoreCase) || text == "0" || text.Equals("N", global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.IsValid = true;
        }

        return result;
    }

    internal static void ApplyInteger(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkStatus target, int field)
    {
        JsonFieldValue value = ReadInteger(source, names, path);
        RequireValid(value, "The TSDR numeric status field is invalid.");
        if (!value.IsPresent)
        {
            return;
        }

        switch (field)
        {
            case 1:
                target.Code = value.Int32Value;
                target.HasCode = true;
                break;
            case 2:
                target.Tm5Code = value.Int32Value;
                target.HasTm5Code = true;
                break;
            case 3:
                target.ActiveClassCount = value.Int32Value;
                target.HasActiveClassCount = true;
                break;
            case 4:
                target.TotalClassCount = value.Int32Value;
                target.HasTotalClassCount = true;
                break;
        }
    }

    internal static void ApplyStatusDate(TrademarkStatus target, TsdrDateValue value, int field)
    {
        switch (field)
        {
            case 1:
                target.StatusDateText = value.Text;
                target.StatusDate = value.Value;
                target.HasStatusDate = value.HasValue;
                break;
            case 2:
                target.FilingDateText = value.Text;
                target.FilingDate = value.Value;
                target.HasFilingDate = value.HasValue;
                break;
            case 3:
                target.RegistrationDateText = value.Text;
                target.RegistrationDate = value.Value;
                target.HasRegistrationDate = value.HasValue;
                break;
            case 4:
                target.AbandonmentDateText = value.Text;
                target.AbandonmentDate = value.Value;
                target.HasAbandonmentDate = value.HasValue;
                break;
            case 5:
                target.CancellationDateText = value.Text;
                target.CancellationDate = value.Value;
                target.HasCancellationDate = value.HasValue;
                break;
            case 6:
                target.RenewalDateText = value.Text;
                target.RenewalDate = value.Value;
                target.HasRenewalDate = value.HasValue;
                break;
        }
    }

    internal static void ApplyMarkBoolean(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkMark target, int field)
    {
        JsonFieldValue value = ReadBoolean(source, names, path);
        RequireValid(value, "The TSDR mark indicator is invalid.");
        if (!value.IsPresent)
        {
            return;
        }

        if (field == 1)
        {
            target.IsStandardCharacter = value.BooleanValue;
            target.HasIsStandardCharacter = true;
        }
        else
        {
            target.IsColorDrawing = value.BooleanValue;
            target.HasIsColorDrawing = true;
        }
    }

    internal static void ApplyBasisBoolean(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkFilingBasis target, int field)
    {
        JsonFieldValue value = ReadBoolean(source, names, path);
        RequireValid(value, "The TSDR filing-basis indicator is invalid.");
        if (!value.IsPresent)
        {
            return;
        }

        switch (field)
        {
            case 1:
                target.Section1A = value.BooleanValue;
                target.HasSection1A = true;
                break;
            case 2:
                target.Section1B = value.BooleanValue;
                target.HasSection1B = true;
                break;
            case 3:
                target.Section44D = value.BooleanValue;
                target.HasSection44D = true;
                break;
            case 4:
                target.Section44E = value.BooleanValue;
                target.HasSection44E = true;
                break;
            case 5:
                target.Section66A = value.BooleanValue;
                target.HasSection66A = true;
                break;
        }
    }

    internal static void ApplyOwnerInteger(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkOwner target, int field)
    {
        JsonFieldValue value = ReadInteger(source, names, path);
        RequireValid(value, "The TSDR owner number is invalid.");
        if (!value.IsPresent)
        {
            return;
        }

        if (field == 1)
        {
            target.SequenceNumber = value.Int32Value;
            target.HasSequenceNumber = true;
        }
        else
        {
            target.PartyTypeCode = value.Int32Value;
            target.HasPartyTypeCode = true;
        }
    }

    internal static void ApplyEventInteger(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkProsecutionEvent target, int field)
    {
        JsonFieldValue value = ReadInteger(source, names, path);
        RequireValid(value, "The TSDR prosecution-history number is invalid.");
        if (!value.IsPresent)
        {
            return;
        }

        if (field == 1)
        {
            target.EntryNumber = value.Int32Value;
            target.HasEntryNumber = true;
        }
        else
        {
            target.SequenceNumber = value.Int32Value;
            target.HasSequenceNumber = true;
        }
    }

    internal static void ApplyEventBoolean(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkProsecutionEvent target)
    {
        JsonFieldValue value = ReadBoolean(source, names, path);
        RequireValid(value, "The TSDR prosecution-history confidentiality indicator is invalid.");
        if (value.IsPresent)
        {
            target.IsConfidential = value.BooleanValue;
            target.HasIsConfidential = true;
        }
    }

    internal static void ApplyEventDate(TrademarkProsecutionEvent target, TsdrDateValue value, int field)
    {
        if (field == 1)
        {
            target.EventDateText = value.Text;
            target.EventDate = value.Value;
            target.HasEventDate = value.HasValue;
        }
        else if (field == 2)
        {
            target.DueDateText = value.Text;
            target.DueDate = value.Value;
            target.HasDueDate = value.HasValue;
        }
        else
        {
            target.RecordedAtText = value.Text;
            target.RecordedAt = value.Value;
            target.HasRecordedAt = value.HasValue;
        }
    }

    internal static void ApplyGoodsDate(TrademarkGoodsAndService target, TsdrDateValue value, int field)
    {
        if (field == 1)
        {
            target.StatusDateText = value.Text;
            target.StatusDate = value.Value;
            target.HasStatusDate = value.HasValue;
        }
        else if (field == 2)
        {
            target.FirstUseDateText = value.Text;
            target.FirstUseDate = value.Value;
            target.HasFirstUseDate = value.HasValue;
        }
        else
        {
            target.FirstUseInCommerceDateText = value.Text;
            target.FirstUseInCommerceDate = value.Value;
            target.HasFirstUseInCommerceDate = value.HasValue;
        }
    }

    internal static void ApplyForeignBoolean(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkForeignRegistration target)
    {
        JsonFieldValue value = ReadBoolean(source, names, path);
        RequireValid(value, "The TSDR foreign-priority indicator is invalid.");
        if (value.IsPresent)
        {
            target.IsPriorityClaimed = value.BooleanValue;
            target.HasIsPriorityClaimed = true;
        }
    }

    internal static void ApplyClassBoolean(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkClass target)
    {
        JsonFieldValue value = ReadBoolean(source, names, path);
        RequireValid(value, "The TSDR prime-class indicator is invalid.");
        if (value.IsPresent)
        {
            target.IsPrime = value.BooleanValue;
            target.HasIsPrime = true;
        }
    }

    internal static void ApplyForeignDate(TrademarkForeignRegistration target, TsdrDateValue value, int field)
    {
        if (field == 1)
        {
            target.FilingDateText = value.Text;
            target.FilingDate = value.Value;
            target.HasFilingDate = value.HasValue;
        }
        else if (field == 2)
        {
            target.RegistrationDateText = value.Text;
            target.RegistrationDate = value.Value;
            target.HasRegistrationDate = value.HasValue;
        }
        else
        {
            target.ExpirationDateText = value.Text;
            target.ExpirationDate = value.Value;
            target.HasExpirationDate = value.HasValue;
        }
    }

    internal static void ApplyCaseBoolean(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkCaseStatus target)
    {
        JsonFieldValue value = ReadBoolean(source, names, path);
        RequireValid(value, "The TSDR international-data indicator is invalid.");
        if (value.IsPresent)
        {
            target.IsInternationalData = value.BooleanValue;
            target.HasIsInternationalData = true;
        }
    }

    internal static void ApplyMadridInteger(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkMadridData target)
    {
        JsonFieldValue value = ReadInteger(source, names, path);
        RequireValid(value, "The TSDR Madrid status code is invalid.");
        if (value.IsPresent)
        {
            target.StatusCode = value.Int32Value;
            target.HasStatusCode = true;
        }
    }

    internal static void ApplyMadridDate(TrademarkMadridData target, TsdrDateValue value, int field)
    {
        switch (field)
        {
            case 1:
                target.OriginalFilingDateText = value.Text;
                target.OriginalFilingDate = value.Value;
                target.HasOriginalFilingDate = value.HasValue;
                break;
            case 2:
                target.StatusDateText = value.Text;
                target.StatusDate = value.Value;
                target.HasStatusDate = value.HasValue;
                break;
            case 3:
                target.ReplyByDateText = value.Text;
                target.ReplyByDate = value.Value;
                target.HasReplyByDate = value.HasValue;
                break;
            case 4:
                target.RenewalDateText = value.Text;
                target.RenewalDate = value.Value;
                target.HasRenewalDate = value.HasValue;
                break;
        }
    }

    internal static void ApplyInternationalInteger(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkInternationalRegistration target)
    {
        JsonFieldValue value = ReadInteger(source, names, path);
        RequireValid(value, "The TSDR international-registration status code is invalid.");
        if (value.IsPresent)
        {
            target.StatusCode = value.Int32Value;
            target.HasStatusCode = true;
        }
    }

    internal static void ApplyInternationalBoolean(global::System.Text.Json.JsonElement source, string[] names, string path, TrademarkInternationalRegistration target, int field)
    {
        JsonFieldValue value = ReadBoolean(source, names, path);
        RequireValid(value, "The TSDR international-registration indicator is invalid.");
        if (!value.IsPresent)
        {
            return;
        }

        if (field == 1)
        {
            target.IsPriorityClaimed = value.BooleanValue;
            target.HasIsPriorityClaimed = true;
        }
        else
        {
            target.FirstRefusal = value.BooleanValue;
            target.HasFirstRefusal = true;
        }
    }

    internal static void ApplyInternationalDate(TrademarkInternationalRegistration target, TsdrDateValue value, int field)
    {
        switch (field)
        {
            case 1:
                target.RegistrationDateText = value.Text;
                target.RegistrationDate = value.Value;
                target.HasRegistrationDate = value.HasValue;
                break;
            case 2:
                target.StatusDateText = value.Text;
                target.StatusDate = value.Value;
                target.HasStatusDate = value.HasValue;
                break;
            case 3:
                target.DesignationNotificationDateText = value.Text;
                target.DesignationNotificationDate = value.Value;
                target.HasDesignationNotificationDate = value.HasValue;
                break;
            case 4:
                target.AutomaticProtectionDateText = value.Text;
                target.AutomaticProtectionDate = value.Value;
                target.HasAutomaticProtectionDate = value.HasValue;
                break;
            case 5:
                target.CancellationDateText = value.Text;
                target.CancellationDate = value.Value;
                target.HasCancellationDate = value.HasValue;
                break;
            case 6:
                target.RenewalDateText = value.Text;
                target.RenewalDate = value.Value;
                target.HasRenewalDate = value.HasValue;
                break;
        }
    }

    internal static void ApplyMadridEventDate(TrademarkMadridEvent target, TsdrDateValue value, int field)
    {
        if (field == 1)
        {
            target.EventDateText = value.Text;
            target.EventDate = value.Value;
            target.HasEventDate = value.HasValue;
        }
        else
        {
            target.RecordedAtText = value.Text;
            target.RecordedAt = value.Value;
            target.HasRecordedAt = value.HasValue;
        }
    }

    internal static global::System.Uri? ParseUri(string? text)
    {
        if (global::System.String.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (text.StartsWith("https://", global::System.StringComparison.OrdinalIgnoreCase) || text.StartsWith("http://", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return new global::System.Uri(text, global::System.UriKind.Absolute);
        }

        return null;
    }

    internal static void CaptureUnknown(global::System.Text.Json.JsonElement source, string[] knownNames, global::System.Collections.Generic.Dictionary<string, string> target)
    {
        foreach (global::System.Text.Json.JsonProperty property in source.EnumerateObject())
        {
            bool known = false;
            int index;
            for (index = 0; index < knownNames.Length; index += 1)
            {
                if (property.Name.Equals(knownNames[index], global::System.StringComparison.OrdinalIgnoreCase))
                {
                    known = true;
                    break;
                }
            }

            if (!known)
            {
                target[property.Name] = property.Value.GetRawText();
            }
        }
    }

    internal static void RequireArray(TsdrJsonElementValue value, string message)
    {
        if (value.Element.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw ParseFailure(message, value.Path);
        }
    }

    internal static void RequireObject(global::System.Text.Json.JsonElement value, string path, string message)
    {
        if (value.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure(message, path);
        }
    }

    internal static void RequireValid(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw ParseFailure(message, value.Path);
        }
    }

    internal static ParseException ParseFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "TSDR case status", path, 0L, false, 0, false, null);
    }
}
