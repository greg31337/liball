namespace uspto;

internal static class OdpPatentApplicationParser
{
    internal static PatentFileWrapper ParseWrapper(global::System.Text.Json.JsonElement element, string path, string expectedApplicationNumber, PatentDataProvenance provenance)
    {
        PatentApplicationSummary summary = OdpJsonParser.ParsePatentApplicationSummary(element, path, provenance);
        if (summary.ApplicationNumber == null || !global::System.String.Equals(summary.ApplicationNumber, expectedApplicationNumber, global::System.StringComparison.Ordinal))
        {
            throw OdpJsonParser.ParseApplicationFailure("The ODP patent application response does not match the requested application.", path + ".applicationNumberText");
        }

        JsonFieldReader record = new JsonFieldReader(element, path);
        JsonFieldValue metadataValue = record.ReadObject("applicationMetaData");
        if (!metadataValue.IsPresent || !metadataValue.IsValid || metadataValue.IsNull)
        {
            throw OdpJsonParser.ParseApplicationFailure("The ODP patent application metadata is missing or invalid.", metadataValue.Path);
        }

        PatentFileWrapper wrapper = new PatentFileWrapper();
        wrapper.ApplicationNumber = summary.ApplicationNumber;
        wrapper.Summary = summary;
        wrapper.Metadata = ParseMetadata(metadataValue.ElementValue, metadataValue.Path, summary);
        wrapper.PublicationFiles = ParseFileCollection(record, "pgpubDocumentMetaData", "PreGrantPublication", summary.PublicationNumber);
        wrapper.GrantFiles = ParseFileCollection(record, "grantDocumentMetaData", "Grant", summary.PatentNumber);
        JsonFieldValue lastIngested = record.ReadDate("lastIngestionDateTime");
        ValidateOptional(lastIngested, "The ODP patent application last-ingestion time is invalid.");
        if (lastIngested.IsPresent && !lastIngested.IsNull)
        {
            wrapper.LastIngestedAt = lastIngested.DateValue;
            wrapper.HasLastIngestedAt = true;
        }

        wrapper.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = record.CaptureUnknownFields(new string[] { "applicationNumberText", "applicationMetaData", "pgpubDocumentMetaData", "grantDocumentMetaData", "lastIngestionDateTime" });
        int unknownIndex;
        for (unknownIndex = 0; unknownIndex < unknown.Length; unknownIndex += 1)
        {
            wrapper.UnknownSections[unknown[unknownIndex].Name] = unknown[unknownIndex].RawJson;
        }

        return wrapper;
    }

    internal static PatentApplicationMetadata ParseMetadata(global::System.Text.Json.JsonElement element, string path, PatentApplicationSummary summary)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentApplicationMetadata metadata = new PatentApplicationMetadata();
        metadata.ApplicationNumber = summary.ApplicationNumber;
        metadata.PatentNumber = OdpJsonParser.ReadOptionalString(reader, "patentNumber");
        metadata.InventionTitle = OdpJsonParser.ReadOptionalString(reader, "inventionTitle");
        metadata.ApplicationTypeCode = OdpJsonParser.ReadOptionalString(reader, "applicationTypeCode");
        metadata.ApplicationTypeLabel = OdpJsonParser.ReadOptionalString(reader, "applicationTypeLabelName");
        metadata.ApplicationTypeCategory = OdpJsonParser.ReadOptionalString(reader, "applicationTypeCategory");
        metadata.DocketNumber = OdpJsonParser.ReadOptionalString(reader, "docketNumber");
        metadata.ConfirmationNumber = OdpJsonParser.ReadOptionalString(reader, "applicationConfirmationNumber");
        metadata.CustomerNumber = OdpJsonParser.ReadOptionalString(reader, "customerNumber");
        metadata.ArtUnit = OdpJsonParser.ReadOptionalString(reader, "groupArtUnitNumber");
        metadata.TechnologyCenter = OdpJsonParser.ReadOptionalString(reader, "technologyCenter");
        metadata.ApplicationStatusDescription = OdpJsonParser.ReadOptionalString(reader, "applicationStatusDescriptionText");
        metadata.FirstApplicantName = OdpJsonParser.ReadOptionalString(reader, "firstApplicantName");
        metadata.FirstInventorName = OdpJsonParser.ReadOptionalString(reader, "firstInventorName");
        metadata.EarliestPublicationNumber = OdpJsonParser.ReadOptionalString(reader, "earliestPublicationNumber");
        metadata.PctApplicationNumber = ReadPctApplicationNumber(reader);
        metadata.PctPublicationNumber = OdpJsonParser.ReadOptionalString(reader, "pctPublicationNumber");
        metadata.InternationalRegistrationNumber = OdpJsonParser.ReadOptionalString(reader, "internationalRegistrationNumber");
        metadata.PublicationNumbers = ReadStringArray(reader, "publicationNumberBag");
        if (metadata.PublicationNumbers.Length == 0 && metadata.EarliestPublicationNumber != null)
        {
            metadata.PublicationNumbers = new string[] { metadata.EarliestPublicationNumber };
        }

        metadata.PublicationSequenceNumbers = ReadStringArray(reader, "publicationSequenceNumberBag");
        metadata.PublicationCategories = ReadStringArray(reader, "publicationCategoryBag");
        metadata.PublicationDates = ReadDateArray(reader, "publicationDateBag");
        ApplyMetadataDates(metadata, reader);
        ApplyMetadataValues(metadata, reader);
        metadata.EntityStatus = ParseEntityStatus(reader);
        metadata.Parties.Applicants = ParseApplicants(reader);
        metadata.Parties.Inventors = ParseInventors(reader);
        string? examinerName = OdpJsonParser.ReadOptionalString(reader, "examinerNameText");
        if (examinerName != null)
        {
            PatentExaminer examiner = new PatentExaminer();
            examiner.Person.FullName = examinerName;
            examiner.ArtUnit = metadata.ArtUnit;
            examiner.TechnologyCenter = metadata.TechnologyCenter;
            metadata.Parties.Examiners = new PatentExaminer[] { examiner };
        }

        metadata.Classifications = ParseClassifications(reader);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownMetadataFields());
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            metadata.UnknownFields[unknown[index].Name] = unknown[index].RawJson;
        }

        return metadata;
    }

    internal static void ApplyMetadataDates(PatentApplicationMetadata metadata, JsonFieldReader reader)
    {
        JsonFieldValue filingDate = reader.ReadDate("filingDate");
        ValidateOptional(filingDate, "The ODP patent application filing date is invalid.");
        if (filingDate.IsPresent && !filingDate.IsNull)
        {
            metadata.FilingDate = filingDate.DateValue;
            metadata.HasFilingDate = true;
        }

        JsonFieldValue effectiveFilingDate = reader.ReadDate("effectiveFilingDate");
        ValidateOptional(effectiveFilingDate, "The ODP patent application effective filing date is invalid.");
        if (effectiveFilingDate.IsPresent && !effectiveFilingDate.IsNull)
        {
            metadata.EffectiveFilingDate = effectiveFilingDate.DateValue;
            metadata.HasEffectiveFilingDate = true;
        }

        JsonFieldValue statusDate = reader.ReadDate("applicationStatusDate");
        ValidateOptional(statusDate, "The ODP patent application status date is invalid.");
        if (statusDate.IsPresent && !statusDate.IsNull)
        {
            metadata.ApplicationStatusDate = statusDate.DateValue;
            metadata.HasApplicationStatusDate = true;
        }

        JsonFieldValue grantDate = reader.ReadDate("grantDate");
        ValidateOptional(grantDate, "The ODP patent application grant date is invalid.");
        if (grantDate.IsPresent && !grantDate.IsNull)
        {
            metadata.GrantDate = grantDate.DateValue;
            metadata.HasGrantDate = true;
        }

        JsonFieldValue earliestPublicationDate = reader.ReadDate("earliestPublicationDate");
        ValidateOptional(earliestPublicationDate, "The ODP patent application earliest publication date is invalid.");
        if (earliestPublicationDate.IsPresent && !earliestPublicationDate.IsNull)
        {
            metadata.EarliestPublicationDate = earliestPublicationDate.DateValue;
            metadata.HasEarliestPublicationDate = true;
        }

        JsonFieldValue pctPublicationDate = reader.ReadDate("pctPublicationDate");
        ValidateOptional(pctPublicationDate, "The ODP patent application PCT publication date is invalid.");
        if (pctPublicationDate.IsPresent && !pctPublicationDate.IsNull)
        {
            metadata.PctPublicationDate = pctPublicationDate.DateValue;
            metadata.HasPctPublicationDate = true;
        }

        JsonFieldValue internationalDate = reader.ReadDate("internationalRegistrationPublicationDate");
        ValidateOptional(internationalDate, "The ODP patent application international registration publication date is invalid.");
        if (internationalDate.IsPresent && !internationalDate.IsNull)
        {
            metadata.InternationalRegistrationPublicationDate = internationalDate.DateValue;
            metadata.HasInternationalRegistrationPublicationDate = true;
        }
    }

    internal static void ApplyMetadataValues(PatentApplicationMetadata metadata, JsonFieldReader reader)
    {
        JsonFieldValue statusCode = reader.ReadInt32("applicationStatusCode");
        ValidateOptional(statusCode, "The ODP patent application status code is invalid.");
        if (statusCode.IsPresent && !statusCode.IsNull)
        {
            metadata.ApplicationStatusCode = statusCode.Int32Value;
            metadata.HasApplicationStatusCode = true;
        }

        JsonFieldValue nationalStage = ReadIndicator(reader, "nationalStageIndicator");
        ValidateOptional(nationalStage, "The ODP patent application national-stage indicator is invalid.");
        if (nationalStage.IsPresent && !nationalStage.IsNull)
        {
            metadata.IsNationalStage = nationalStage.BooleanValue;
            metadata.HasIsNationalStage = true;
        }

        JsonFieldValue firstInventor = ReadIndicator(reader, "firstInventorToFileIndicator");
        ValidateOptional(firstInventor, "The ODP patent application first-inventor-to-file indicator is invalid.");
        if (firstInventor.IsPresent && !firstInventor.IsNull)
        {
            metadata.IsFirstInventorToFile = firstInventor.BooleanValue;
            metadata.HasIsFirstInventorToFile = true;
        }
    }

    internal static PatentEntityStatus ParseEntityStatus(JsonFieldReader metadata)
    {
        PatentEntityStatus status = new PatentEntityStatus();
        JsonFieldValue entityValue = metadata.ReadObject("entityStatusData");
        if (!entityValue.IsValid)
        {
            throw OdpJsonParser.ParseApplicationFailure("The ODP patent application entity status is invalid.", entityValue.Path);
        }

        if (!entityValue.IsPresent || entityValue.IsNull)
        {
            return status;
        }

        JsonFieldReader entity = new JsonFieldReader(entityValue.ElementValue, entityValue.Path);
        status.Category = OdpJsonParser.ReadOptionalString(entity, "businessEntityStatusCategory");
        JsonFieldValue small = ReadIndicator(entity, "smallEntityStatusIndicator");
        ValidateOptional(small, "The ODP patent application small-entity indicator is invalid.");
        if (small.IsPresent && !small.IsNull)
        {
            status.IsSmallEntity = small.BooleanValue;
            status.HasIsSmallEntity = true;
        }

        JsonFieldValue micro = ReadIndicator(entity, "microEntityStatusIndicator");
        ValidateOptional(micro, "The ODP patent application micro-entity indicator is invalid.");
        if (micro.IsPresent && !micro.IsNull)
        {
            status.IsMicroEntity = micro.BooleanValue;
            status.HasIsMicroEntity = true;
        }

        return status;
    }

    internal static PatentApplicant[] ParseApplicants(JsonFieldReader metadata)
    {
        JsonFieldValue bag = metadata.ReadArray("applicantBag");
        ValidateOptional(bag, "The ODP patent application applicant collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentApplicant[0];
        }

        PatentApplicant[] applicants = new PatentApplicant[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw OdpJsonParser.ParseApplicationFailure("The ODP patent application applicant is invalid.", bag.ArrayItemPaths[index]);
            }

            JsonFieldReader applicantReader = new JsonFieldReader(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
            PatentApplicant applicant = new PatentApplicant();
            string? fullName = OdpJsonParser.ReadOptionalString(applicantReader, "applicantNameText");
            PatentPerson person = ParsePerson(applicantReader, fullName);
            if (HasPersonData(person))
            {
                applicant.Person = person;
            }
            else if (fullName != null)
            {
                applicant.Organization = new PatentOrganization();
                applicant.Organization.Name = fullName;
            }

            string? organizationName = OdpJsonParser.ReadOptionalString(applicantReader, "organizationName");
            if (organizationName != null)
            {
                applicant.Organization = new PatentOrganization();
                applicant.Organization.Name = organizationName;
            }

            applicant.Addresses = ParseAddresses(applicantReader, "correspondenceAddressBag");
            applicant.ResidenceCountryCode = OdpJsonParser.ReadOptionalString(applicantReader, "countryCode");
            applicant.ApplicantCategory = OdpJsonParser.ReadOptionalString(applicantReader, "applicantCategory");
            applicant.Authority = OdpJsonParser.ReadOptionalString(applicantReader, "applicantAuthorityCategory");
            JsonFieldValue sequence = applicantReader.ReadInt32("applicantSequenceNumber");
            ValidateOptional(sequence, "The ODP patent application applicant sequence is invalid.");
            if (sequence.IsPresent && !sequence.IsNull)
            {
                applicant.Sequence = sequence.Int32Value;
                applicant.HasSequence = true;
            }

            JsonFieldValue inventor = ReadIndicator(applicantReader, "inventorIndicator");
            ValidateOptional(inventor, "The ODP patent application applicant inventor indicator is invalid.");
            if (inventor.IsPresent && !inventor.IsNull)
            {
                applicant.IsInventor = inventor.BooleanValue;
                applicant.HasIsInventor = true;
            }

            applicants[index] = applicant;
        }

        return applicants;
    }

    internal static PatentInventor[] ParseInventors(JsonFieldReader metadata)
    {
        JsonFieldValue bag = metadata.ReadArray("inventorBag");
        ValidateOptional(bag, "The ODP patent application inventor collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentInventor[0];
        }

        PatentInventor[] inventors = new PatentInventor[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw OdpJsonParser.ParseApplicationFailure("The ODP patent application inventor is invalid.", bag.ArrayItemPaths[index]);
            }

            JsonFieldReader inventorReader = new JsonFieldReader(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
            PatentInventor inventor = new PatentInventor();
            inventor.Person = ParsePerson(inventorReader, OdpJsonParser.ReadOptionalString(inventorReader, "inventorNameText"));
            inventor.Addresses = ParseAddresses(inventorReader, "correspondenceAddressBag");
            inventor.ResidenceCountryCode = OdpJsonParser.ReadOptionalString(inventorReader, "countryCode");
            JsonFieldValue sequence = inventorReader.ReadInt32("inventorSequenceNumber");
            ValidateOptional(sequence, "The ODP patent application inventor sequence is invalid.");
            if (sequence.IsPresent && !sequence.IsNull)
            {
                inventor.Sequence = sequence.Int32Value;
                inventor.HasSequence = true;
            }

            JsonFieldValue deceased = ReadIndicator(inventorReader, "deceasedIndicator");
            ValidateOptional(deceased, "The ODP patent application deceased-inventor indicator is invalid.");
            if (deceased.IsPresent && !deceased.IsNull)
            {
                inventor.IsDeceased = deceased.BooleanValue;
                inventor.HasIsDeceased = true;
            }

            inventors[index] = inventor;
        }

        return inventors;
    }

    internal static PatentPerson ParsePerson(JsonFieldReader reader, string? fullName)
    {
        PatentPerson person = new PatentPerson();
        person.FullName = fullName;
        person.FirstName = OdpJsonParser.ReadOptionalString(reader, "firstName");
        person.MiddleName = OdpJsonParser.ReadOptionalString(reader, "middleName");
        person.LastName = OdpJsonParser.ReadOptionalString(reader, "lastName");
        person.PreferredName = OdpJsonParser.ReadOptionalString(reader, "preferredName");
        person.NamePrefix = OdpJsonParser.ReadOptionalString(reader, "namePrefix");
        person.NameSuffix = OdpJsonParser.ReadOptionalString(reader, "nameSuffix");
        return person;
    }

    internal static bool HasPersonData(PatentPerson person)
    {
        return person.FirstName != null || person.MiddleName != null || person.LastName != null || person.PreferredName != null || person.NamePrefix != null || person.NameSuffix != null;
    }

    internal static PatentAddress[] ParseAddresses(JsonFieldReader reader, string fieldName)
    {
        JsonFieldValue bag = reader.ReadArray(fieldName);
        ValidateOptional(bag, "The ODP patent application address collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentAddress[0];
        }

        PatentAddress[] addresses = new PatentAddress[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw OdpJsonParser.ParseApplicationFailure("The ODP patent application address is invalid.", bag.ArrayItemPaths[index]);
            }

            JsonFieldReader addressReader = new JsonFieldReader(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
            PatentAddress address = new PatentAddress();
            address.NameLineOne = OdpJsonParser.ReadOptionalString(addressReader, "nameLineOneText");
            address.NameLineTwo = OdpJsonParser.ReadOptionalString(addressReader, "nameLineTwoText");
            address.AddressLineOne = OdpJsonParser.ReadOptionalString(addressReader, "addressLineOneText");
            address.AddressLineTwo = OdpJsonParser.ReadOptionalString(addressReader, "addressLineTwoText");
            address.AddressLineThree = OdpJsonParser.ReadOptionalString(addressReader, "addressLineThreeText");
            address.AddressLineFour = OdpJsonParser.ReadOptionalString(addressReader, "addressLineFourText");
            address.City = OdpJsonParser.ReadOptionalString(addressReader, "cityName");
            address.GeographicRegionName = OdpJsonParser.ReadOptionalString(addressReader, "geographicRegionName");
            address.GeographicRegionCode = OdpJsonParser.ReadOptionalString(addressReader, "geographicRegionCode");
            address.PostalCode = OdpJsonParser.ReadOptionalString(addressReader, "postalCode");
            address.CountryCode = OdpJsonParser.ReadOptionalString(addressReader, "countryCode");
            address.CountryName = OdpJsonParser.ReadOptionalString(addressReader, "countryName");
            address.PostalAddressCategory = OdpJsonParser.ReadOptionalString(addressReader, "postalAddressCategory");
            address.EmailAddress = OdpJsonParser.ReadOptionalString(addressReader, "emailAddress");
            addresses[index] = address;
        }

        return addresses;
    }

    internal static PatentClassificationData ParseClassifications(JsonFieldReader metadata)
    {
        PatentClassificationData classifications = new PatentClassificationData();
        classifications.Cpc = ParseCpcClassifications(metadata);
        string? uspcClass = OdpJsonParser.ReadOptionalString(metadata, "class");
        string? uspcSubclass = OdpJsonParser.ReadOptionalString(metadata, "subclass");
        string? uspcSymbol = OdpJsonParser.ReadOptionalString(metadata, "uspcSymbolText");
        if (uspcClass != null || uspcSubclass != null || uspcSymbol != null)
        {
            PatentUspcClassification uspc = new PatentUspcClassification();
            uspc.Class = uspcClass;
            uspc.Subclass = uspcSubclass;
            uspc.Symbol = uspcSymbol;
            classifications.Uspc = new PatentUspcClassification[] { uspc };
        }

        return classifications;
    }

    internal static PatentCpcClassification[] ParseCpcClassifications(JsonFieldReader metadata)
    {
        JsonFieldValue bag = metadata.ReadArray("cpcClassificationBag");
        ValidateOptional(bag, "The ODP patent application CPC collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentCpcClassification[0];
        }

        PatentCpcClassification[] classifications = new PatentCpcClassification[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            PatentCpcClassification classification = new PatentCpcClassification();
            global::System.Text.Json.JsonElement item = bag.ArrayItems[index];
            if (item.ValueKind == global::System.Text.Json.JsonValueKind.String)
            {
                classification.Symbol = item.GetString();
            }
            else if (item.ValueKind == global::System.Text.Json.JsonValueKind.Object)
            {
                JsonFieldReader itemReader = new JsonFieldReader(item, bag.ArrayItemPaths[index]);
                classification.Symbol = OdpJsonParser.ReadOptionalString(itemReader, "cpcClassificationSymbolText");
                if (classification.Symbol == null)
                {
                    classification.Symbol = OdpJsonParser.ReadOptionalString(itemReader, "symbol");
                }

                classification.Section = OdpJsonParser.ReadOptionalString(itemReader, "section");
                classification.Class = OdpJsonParser.ReadOptionalString(itemReader, "class");
                classification.Subclass = OdpJsonParser.ReadOptionalString(itemReader, "subclass");
                classification.MainGroup = OdpJsonParser.ReadOptionalString(itemReader, "mainGroup");
                classification.Subgroup = OdpJsonParser.ReadOptionalString(itemReader, "subgroup");
                classification.Version = OdpJsonParser.ReadOptionalString(itemReader, "version");
                JsonFieldValue sequence = itemReader.ReadInt32("sequenceNumber");
                ValidateOptional(sequence, "The ODP patent application CPC sequence is invalid.");
                if (sequence.IsPresent && !sequence.IsNull)
                {
                    classification.Sequence = sequence.Int32Value;
                    classification.HasSequence = true;
                }

                JsonFieldValue inventive = ReadIndicator(itemReader, "inventiveIndicator");
                ValidateOptional(inventive, "The ODP patent application CPC inventive indicator is invalid.");
                if (inventive.IsPresent && !inventive.IsNull)
                {
                    classification.IsInventive = inventive.BooleanValue;
                    classification.HasIsInventive = true;
                }

                JsonFieldValue primary = ReadIndicator(itemReader, "primaryIndicator");
                ValidateOptional(primary, "The ODP patent application CPC primary indicator is invalid.");
                if (primary.IsPresent && !primary.IsNull)
                {
                    classification.IsPrimary = primary.BooleanValue;
                    classification.HasIsPrimary = true;
                }
            }
            else
            {
                throw OdpJsonParser.ParseApplicationFailure("The ODP patent application CPC classification is invalid.", bag.ArrayItemPaths[index]);
            }

            ApplyCpcSymbolParts(classification);
            classifications[index] = classification;
        }

        return classifications;
    }

    internal static void ApplyCpcSymbolParts(PatentCpcClassification classification)
    {
        string? symbol = classification.Symbol;
        if (global::System.String.IsNullOrWhiteSpace(symbol))
        {
            return;
        }

        string compact = symbol.Replace(" ", string.Empty);
        if (classification.Section == null && compact.Length >= 1)
        {
            classification.Section = compact.Substring(0, 1);
        }

        if (classification.Class == null && compact.Length >= 3)
        {
            classification.Class = compact.Substring(0, 3);
        }

        if (classification.Subclass == null && compact.Length >= 4)
        {
            classification.Subclass = compact.Substring(0, 4);
        }

        int slash = compact.IndexOf('/');
        if (slash > 4)
        {
            if (classification.MainGroup == null)
            {
                classification.MainGroup = compact.Substring(4, slash - 4);
            }

            if (classification.Subgroup == null && slash + 1 < compact.Length)
            {
                classification.Subgroup = compact.Substring(slash + 1);
            }
        }
    }

    internal static PatentFileMetadata[] ParseFileCollection(JsonFieldReader record, string fieldName, string category, string? documentIdentifier)
    {
        JsonFieldValue value = record.ReadRaw(fieldName);
        if (!value.IsPresent || value.IsNull)
        {
            return new PatentFileMetadata[0];
        }

        if (value.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            return new PatentFileMetadata[] { ParseFileItem(value.ElementValue, value.Path, category, documentIdentifier) };
        }

        if (value.ElementValue.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw OdpJsonParser.ParseApplicationFailure("The ODP patent application file metadata is invalid.", value.Path);
        }

        global::System.Collections.Generic.List<PatentFileMetadata> files = new global::System.Collections.Generic.List<PatentFileMetadata>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in value.ElementValue.EnumerateArray())
        {
            string itemPath = value.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            files.Add(ParseFileItem(item, itemPath, category, documentIdentifier));
            index += 1;
        }

        return files.ToArray();
    }

    internal static PatentFileMetadata ParseFileItem(global::System.Text.Json.JsonElement element, string path, string category, string? documentIdentifier)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw OdpJsonParser.ParseApplicationFailure("The ODP patent application file metadata entry is invalid.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentFileMetadata file = new PatentFileMetadata();
        file.Category = category;
        file.DocumentIdentifier = documentIdentifier;
        file.ProductIdentifier = OdpJsonParser.ReadOptionalString(reader, "productIdentifier");
        file.ArchiveFileName = OdpJsonParser.ReadOptionalString(reader, "zipFileName");
        file.XmlFileName = OdpJsonParser.ReadOptionalString(reader, "xmlFileName");
        file.MediaType = OdpJsonParser.ReadOptionalString(reader, "fileMediaType");
        string? location = OdpJsonParser.ReadOptionalString(reader, "fileLocationURI");
        if (location != null)
        {
            if (!global::System.Uri.IsWellFormedUriString(location, global::System.UriKind.Absolute))
            {
                throw OdpJsonParser.ParseApplicationFailure("The ODP patent application file location is invalid.", path + ".fileLocationURI");
            }

            file.LocationUri = new global::System.Uri(location);
        }

        JsonFieldValue created = ReadFileDate(reader);
        ValidateOptional(created, "The ODP patent application file creation time is invalid.");
        if (created.IsPresent && !created.IsNull)
        {
            file.CreatedAt = created.DateValue;
            file.HasCreatedAt = true;
        }

        JsonFieldValue size = reader.ReadInt64("fileSize");
        ValidateOptional(size, "The ODP patent application file size is invalid.");
        if (size.IsPresent && !size.IsNull)
        {
            if (size.Int64Value < 0L)
            {
                throw OdpJsonParser.ParseApplicationFailure("The ODP patent application file size cannot be negative.", size.Path);
            }

            file.Size = size.Int64Value;
            file.HasSize = true;
        }

        file.Format = ResponseReader.DetectDocumentFormat(file.MediaType);
        if (file.Format == DocumentFormat.Unknown && file.ArchiveFileName != null && file.ArchiveFileName.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase))
        {
            file.Format = DocumentFormat.Zip;
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "productIdentifier", "zipFileName", "xmlFileName", "fileLocationURI", "fileMediaType", "fileCreateDateTime", "fileSize" });
        int unknownIndex;
        for (unknownIndex = 0; unknownIndex < unknown.Length; unknownIndex += 1)
        {
            file.UnknownFields[unknown[unknownIndex].Name] = unknown[unknownIndex].RawJson;
        }

        return file;
    }

    internal static JsonFieldValue ReadFileDate(JsonFieldReader reader)
    {
        JsonFieldValue value = reader.ReadDate("fileCreateDateTime");
        if (value.IsValid || !value.IsPresent || value.IsNull)
        {
            return value;
        }

        JsonFieldValue text = reader.ReadString("fileCreateDateTime");
        if (!text.IsValid || text.StringValue == null)
        {
            return value;
        }

        string source = text.StringValue.Trim();
        if (source.Length >= 19 && source[4] == '-' && source[7] == '-' && source[10] == ':')
        {
            string normalized = source.Substring(0, 10) + "T" + source.Substring(11);
            JsonDateParseResult parsed = JsonFieldReader.ParseFlexibleDate(normalized);
            if (parsed.IsValid)
            {
                value.DateValue = parsed.Value;
                value.IsValid = true;
            }
        }

        return value;
    }

    internal static string? ReadPctApplicationNumber(JsonFieldReader reader)
    {
        JsonFieldValue value = reader.ReadString("pctApplicationNumber");
        ValidateOptional(value, "The ODP patent application PCT application number is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        PctApplicationNumberResult parsed = PatentIdentifierParser.ParsePctApplicationNumber(value.StringValue ?? string.Empty);
        if (!parsed.IsValid)
        {
            throw OdpJsonParser.ParseApplicationFailure("The ODP patent application PCT application number is invalid.", value.Path);
        }

        return parsed.CanonicalNumber;
    }

    internal static string[] ReadStringArray(JsonFieldReader reader, string fieldName)
    {
        JsonFieldValue value = reader.ReadArray(fieldName);
        ValidateOptional(value, "The ODP patent application string collection is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return new string[0];
        }

        string[] result = new string[value.ArrayItems.Length];
        int index;
        for (index = 0; index < value.ArrayItems.Length; index += 1)
        {
            global::System.Text.Json.JsonElement item = value.ArrayItems[index];
            if (item.ValueKind == global::System.Text.Json.JsonValueKind.String)
            {
                result[index] = item.GetString() ?? string.Empty;
            }
            else if (item.ValueKind == global::System.Text.Json.JsonValueKind.Number || item.ValueKind == global::System.Text.Json.JsonValueKind.True || item.ValueKind == global::System.Text.Json.JsonValueKind.False)
            {
                result[index] = item.GetRawText();
            }
            else
            {
                throw OdpJsonParser.ParseApplicationFailure("The ODP patent application string collection contains an invalid value.", value.ArrayItemPaths[index]);
            }
        }

        return result;
    }

    internal static global::System.DateTimeOffset[] ReadDateArray(JsonFieldReader reader, string fieldName)
    {
        JsonFieldValue value = reader.ReadArray(fieldName);
        ValidateOptional(value, "The ODP patent application date collection is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return new global::System.DateTimeOffset[0];
        }

        global::System.DateTimeOffset[] result = new global::System.DateTimeOffset[value.ArrayItems.Length];
        int index;
        for (index = 0; index < value.ArrayItems.Length; index += 1)
        {
            JsonDateParseResult parsed = JsonFieldReader.ParseDateElement(value.ArrayItems[index]);
            if (!parsed.IsValid)
            {
                throw OdpJsonParser.ParseApplicationFailure("The ODP patent application date collection contains an invalid value.", value.ArrayItemPaths[index]);
            }

            result[index] = parsed.Value;
        }

        return result;
    }

    internal static JsonFieldValue ReadIndicator(JsonFieldReader reader, string fieldName)
    {
        JsonFieldValue value = reader.ReadBoolean(fieldName);
        if (value.IsValid || !value.IsPresent)
        {
            return value;
        }

        JsonFieldValue text = reader.ReadString(fieldName);
        if (!text.IsValid || text.IsNull || text.StringValue == null)
        {
            return value;
        }

        string normalized = text.StringValue.Trim();
        if (normalized.Equals("Y", global::System.StringComparison.OrdinalIgnoreCase) || normalized.Equals("YES", global::System.StringComparison.OrdinalIgnoreCase))
        {
            value.BooleanValue = true;
            value.IsValid = true;
        }
        else if (normalized.Equals("N", global::System.StringComparison.OrdinalIgnoreCase) || normalized.Equals("NO", global::System.StringComparison.OrdinalIgnoreCase))
        {
            value.BooleanValue = false;
            value.IsValid = true;
        }

        return value;
    }

    internal static void ValidateOptional(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw OdpJsonParser.ParseApplicationFailure(message, value.Path);
        }
    }

    internal static string[] KnownMetadataFields()
    {
        return new string[]
        {
            "nationalStageIndicator",
            "entityStatusData",
            "publicationNumberBag",
            "publicationDateBag",
            "publicationSequenceNumberBag",
            "publicationCategoryBag",
            "docketNumber",
            "firstInventorToFileIndicator",
            "firstApplicantName",
            "firstInventorName",
            "applicationConfirmationNumber",
            "applicationStatusDate",
            "applicationStatusDescriptionText",
            "filingDate",
            "effectiveFilingDate",
            "grantDate",
            "groupArtUnitNumber",
            "technologyCenter",
            "applicationTypeCode",
            "applicationTypeLabelName",
            "applicationTypeCategory",
            "inventionTitle",
            "patentNumber",
            "applicationStatusCode",
            "earliestPublicationNumber",
            "earliestPublicationDate",
            "pctApplicationNumber",
            "pctPublicationNumber",
            "pctPublicationDate",
            "internationalRegistrationPublicationDate",
            "internationalRegistrationNumber",
            "examinerNameText",
            "class",
            "subclass",
            "uspcSymbolText",
            "customerNumber",
            "cpcClassificationBag",
            "applicantBag",
            "inventorBag"
        };
    }
}
