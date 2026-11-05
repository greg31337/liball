namespace uspto;

internal class PatentXmlElement
{
    internal string Name = string.Empty;
    internal global::System.Collections.Generic.Dictionary<string, string> Attributes = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
    internal global::System.Collections.Generic.List<PatentXmlElement> Children = new global::System.Collections.Generic.List<PatentXmlElement>();
    internal global::System.Collections.Generic.List<PatentXmlContent> Content = new global::System.Collections.Generic.List<PatentXmlContent>();
}

internal class PatentXmlContent
{
    internal string? Text = null;
    internal PatentXmlElement? Element = null;
}

internal static class PatentFullTextXmlParser
{
    internal static PatentFullText Parse(global::System.IO.Stream input, string? sourceName, UsptoLimits limits, PatentDataProvenance provenance)
    {
        if (provenance == null)
        {
            throw new ValidationException("Patent full-text provenance is required.", "provenance", "REQUIRED", null);
        }

        SecureXmlReader reader = new SecureXmlReader(input, sourceName, limits, true);
        global::System.Collections.Generic.List<PatentXmlElement> stack = new global::System.Collections.Generic.List<PatentXmlElement>();
        PatentXmlElement? root = null;
        while (reader.Read())
        {
            if (reader.Current.NodeType == global::System.Xml.XmlNodeType.Element)
            {
                PatentXmlElement element = CreateElement(reader.Current);
                if (stack.Count > 0)
                {
                    stack[stack.Count - 1].Children.Add(element);
                    PatentXmlContent content = new PatentXmlContent();
                    content.Element = element;
                    stack[stack.Count - 1].Content.Add(content);
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
            else if (reader.Current.NodeType == global::System.Xml.XmlNodeType.Text || reader.Current.NodeType == global::System.Xml.XmlNodeType.CDATA || reader.Current.NodeType == global::System.Xml.XmlNodeType.SignificantWhitespace || reader.Current.NodeType == global::System.Xml.XmlNodeType.Whitespace)
            {
                if (stack.Count > 0)
                {
                    PatentXmlContent content = new PatentXmlContent();
                    content.Text = reader.Current.Value;
                    stack[stack.Count - 1].Content.Add(content);
                }
            }
            else if (reader.Current.NodeType == global::System.Xml.XmlNodeType.EndElement && stack.Count > 0)
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }

        reader.Close();
        if (root == null)
        {
            throw ParseFailure("The patent full-text XML does not contain a document root.", sourceName, "/");
        }

        bool isGrant = root.Name.Equals("us-patent-grant", global::System.StringComparison.Ordinal);
        bool isApplication = root.Name.Equals("us-patent-application", global::System.StringComparison.Ordinal);
        if (!isGrant && !isApplication)
        {
            throw ParseFailure("The XML root is not a USPTO patent grant or published application.", sourceName, "/" + root.Name);
        }

        PatentFullText result = new PatentFullText();
        result.RootElementName = root.Name;
        result.DocumentCategory = isGrant ? "grant" : "published application";
        result.RootIdentifier = ReadAttribute(root, "id");
        result.SourceFileName = ReadAttribute(root, "file");
        result.DtdVersion = ReadAttribute(root, "dtd-version");
        result.LanguageCode = ReadAttribute(root, "lang");
        result.CountryCode = ReadAttribute(root, "country");
        result.ProducedDateText = ReadAttribute(root, "date-produced");
        result.PublishedDateText = ReadAttribute(root, "date-publ");
        ApplyRootDates(result);
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);

        string bibliographicName = isGrant ? "us-bibliographic-data-grant" : "us-bibliographic-data-application";
        PatentXmlElement? bibliographic = FindFirstDescendant(root, bibliographicName);
        if (bibliographic != null)
        {
            result.BibliographicData = ParseBibliographicData(bibliographic);
        }

        result.Abstract = ParseTextSection(root, "abstract");
        result.Description = ParseTextSection(root, "description");
        result.Claims = ParseClaims(root);
        result.ExternalResources = ParseExternalResources(root);
        return result;
    }

    internal static PatentXmlElement CreateElement(SecureXmlNode node)
    {
        PatentXmlElement result = new PatentXmlElement();
        result.Name = node.LocalName;
        foreach (SecureXmlAttribute attribute in node.Attributes)
        {
            result.Attributes[attribute.LocalName] = attribute.Value;
        }

        return result;
    }

    internal static PatentBibliographicData ParseBibliographicData(PatentXmlElement element)
    {
        PatentBibliographicData result = new PatentBibliographicData();
        PatentXmlElement? publicationReference = FindFirstDescendant(element, "publication-reference");
        PatentXmlElement? applicationReference = FindFirstDescendant(element, "application-reference");
        result.Publication = ParseReferenceIdentifier(publicationReference);
        result.Application = ParseReferenceIdentifier(applicationReference);
        if (applicationReference != null)
        {
            result.ApplicationType = ReadAttribute(applicationReference, "appl-type");
        }

        result.ApplicationSeriesCode = ReadFirstText(element, "us-application-series-code");
        result.InventionTitle = ReadFirstText(element, "invention-title");
        ApplyClaimCount(result, ReadFirstText(element, "number-of-claims"));
        result.Parties = ParseParties(element);
        result.Classifications = ParseClassifications(element);
        result.Citations = ParseCitations(element);
        result.RelatedApplications = ParseRelatedApplications(element);
        ApplyGovernmentInterests(result.Classifications, element);
        return result;
    }

    internal static PatentDocumentIdentifier ParseReferenceIdentifier(PatentXmlElement? reference)
    {
        if (reference == null)
        {
            return new PatentDocumentIdentifier();
        }

        PatentXmlElement? identifier = FindFirstDescendant(reference, "document-id");
        return ParseDocumentIdentifier(identifier);
    }

    internal static PatentDocumentIdentifier ParseDocumentIdentifier(PatentXmlElement? element)
    {
        PatentDocumentIdentifier result = new PatentDocumentIdentifier();
        if (element == null)
        {
            return result;
        }

        result.CountryCode = ReadFirstText(element, "country");
        result.DocumentNumber = ReadFirstText(element, "doc-number");
        result.KindCode = ReadFirstText(element, "kind");
        result.Name = ReadFirstText(element, "name");
        result.DateText = ReadFirstText(element, "date");
        JsonDateParseResult parsed = JsonFieldReader.ParseFlexibleDate(result.DateText == null ? string.Empty : result.DateText);
        if (parsed.IsValid)
        {
            result.Date = parsed.Value;
            result.HasDate = true;
        }

        return result;
    }

    internal static PatentTextBlock[] ParseTextSection(PatentXmlElement root, string sectionName)
    {
        PatentXmlElement? section = FindFirstDescendant(root, sectionName);
        if (section == null)
        {
            return new PatentTextBlock[0];
        }

        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectTextElements(section, elements);
        global::System.Collections.Generic.List<PatentTextBlock> blocks = new global::System.Collections.Generic.List<PatentTextBlock>();
        foreach (PatentXmlElement element in elements)
        {
            PatentTextBlock block = new PatentTextBlock();
            block.Identifier = ReadAttribute(element, "id");
            block.Number = ReadAttribute(element, "num");
            block.Category = element.Name;
            block.Level = ReadAttribute(element, "level");
            block.Text = ReadNormalizedText(element);
            blocks.Add(block);
        }

        if (blocks.Count == 0 && ReadNormalizedText(section).Length > 0)
        {
            PatentTextBlock block = new PatentTextBlock();
            block.Identifier = ReadAttribute(section, "id");
            block.Category = sectionName;
            block.Text = ReadNormalizedText(section);
            blocks.Add(block);
        }

        return blocks.ToArray();
    }

    internal static void CollectTextElements(PatentXmlElement element, global::System.Collections.Generic.List<PatentXmlElement> results)
    {
        foreach (PatentXmlElement child in element.Children)
        {
            if (child.Name.Equals("p", global::System.StringComparison.Ordinal) || child.Name.Equals("heading", global::System.StringComparison.Ordinal))
            {
                results.Add(child);
            }
            else
            {
                CollectTextElements(child, results);
            }
        }
    }

    internal static PatentClaim[] ParseClaims(PatentXmlElement root)
    {
        PatentXmlElement? claims = FindFirstDescendant(root, "claims");
        if (claims == null)
        {
            return new PatentClaim[0];
        }

        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(claims, "claim", elements);
        global::System.Collections.Generic.List<PatentClaim> results = new global::System.Collections.Generic.List<PatentClaim>();
        foreach (PatentXmlElement element in elements)
        {
            PatentClaim claim = new PatentClaim();
            claim.Identifier = ReadAttribute(element, "id");
            claim.Number = ReadAttribute(element, "num");
            if (claim.Number == null)
            {
                claim.Number = ReadFirstText(element, "claim-number");
            }

            claim.Text = ReadNormalizedClaimText(element);
            global::System.Collections.Generic.List<PatentXmlElement> references = new global::System.Collections.Generic.List<PatentXmlElement>();
            CollectNamedDescendants(element, "claim-ref", references);
            global::System.Collections.Generic.List<PatentClaimReference> mapped = new global::System.Collections.Generic.List<PatentClaimReference>();
            foreach (PatentXmlElement reference in references)
            {
                PatentClaimReference item = new PatentClaimReference();
                item.Identifier = ReadAttribute(reference, "idref");
                item.Text = ReadNormalizedText(reference);
                mapped.Add(item);
            }

            claim.References = mapped.ToArray();
            results.Add(claim);
        }

        return results.ToArray();
    }

    internal static PatentCitation[] ParseCitations(PatentXmlElement bibliographic)
    {
        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(bibliographic, "us-citation", elements);
        CollectNamedDescendants(bibliographic, "citation", elements);
        global::System.Collections.Generic.List<PatentCitation> results = new global::System.Collections.Generic.List<PatentCitation>();
        foreach (PatentXmlElement element in elements)
        {
            PatentCitation citation = new PatentCitation();
            PatentXmlElement? patent = FindFirstDescendant(element, "patcit");
            PatentXmlElement? nonPatent = FindFirstDescendant(element, "nplcit");
            if (patent != null)
            {
                citation.Number = ReadAttribute(patent, "num");
                citation.PatentDocument = ParseReferenceIdentifier(patent);
            }

            if (nonPatent != null)
            {
                citation.Number = ReadAttribute(nonPatent, "num");
                citation.NonPatentText = ReadFirstText(nonPatent, "othercit");
            }

            citation.Category = ReadFirstText(element, "category");
            citation.RelatedClaims = ReadFirstText(element, "rel-claims");
            global::System.Collections.Generic.List<string> classifications = new global::System.Collections.Generic.List<string>();
            CollectTextsByName(element, "classification-cpc-text", classifications);
            CollectTextsByName(element, "classification-national", classifications);
            citation.ClassificationText = classifications.ToArray();
            results.Add(citation);
        }

        return results.ToArray();
    }

    internal static PatentRelatedApplication[] ParseRelatedApplications(PatentXmlElement bibliographic)
    {
        PatentXmlElement? related = FindFirstDescendant(bibliographic, "us-related-documents");
        if (related == null)
        {
            related = FindFirstDescendant(bibliographic, "related-documents");
        }

        if (related == null)
        {
            return new PatentRelatedApplication[0];
        }

        global::System.Collections.Generic.List<PatentRelatedApplication> results = new global::System.Collections.Generic.List<PatentRelatedApplication>();
        foreach (PatentXmlElement child in related.Children)
        {
            PatentRelatedApplication item = new PatentRelatedApplication();
            item.Relationship = child.Name;
            PatentXmlElement? parent = FindFirstDescendant(child, "parent-doc");
            PatentXmlElement? childDocument = FindFirstDescendant(child, "child-doc");
            if (parent != null)
            {
                item.ParentDocument = ParseReferenceIdentifier(parent);
            }

            if (childDocument != null)
            {
                item.ChildDocument = ParseReferenceIdentifier(childDocument);
            }

            if (parent == null && childDocument == null)
            {
                item.Document = ParseReferenceIdentifier(child);
            }

            results.Add(item);
        }

        return results.ToArray();
    }

    internal static PatentClassificationData ParseClassifications(PatentXmlElement bibliographic)
    {
        PatentClassificationData result = new PatentClassificationData();
        global::System.Collections.Generic.List<PatentCpcClassification> cpc = new global::System.Collections.Generic.List<PatentCpcClassification>();
        PatentXmlElement? mainCpc = FindFirstDescendant(bibliographic, "main-cpc");
        PatentXmlElement? furtherCpc = FindFirstDescendant(bibliographic, "further-cpc");
        ParseCpcContainer(mainCpc, true, cpc);
        ParseCpcContainer(furtherCpc, false, cpc);
        result.Cpc = cpc.ToArray();

        PatentXmlElement? ipcContainer = FindFirstDescendant(bibliographic, "classifications-ipcr");
        global::System.Collections.Generic.List<PatentXmlElement> ipcElements = new global::System.Collections.Generic.List<PatentXmlElement>();
        if (ipcContainer != null)
        {
            CollectNamedDescendants(ipcContainer, "classification-ipcr", ipcElements);
        }

        global::System.Collections.Generic.List<PatentIpcClassification> ipc = new global::System.Collections.Generic.List<PatentIpcClassification>();
        foreach (PatentXmlElement element in ipcElements)
        {
            PatentIpcClassification item = new PatentIpcClassification();
            item.Section = ReadFirstText(element, "section");
            item.Class = ReadFirstText(element, "class");
            item.Subclass = ReadFirstText(element, "subclass");
            item.MainGroup = ReadFirstText(element, "main-group");
            item.Subgroup = ReadFirstText(element, "subgroup");
            item.Symbol = BuildClassificationSymbol(item.Section, item.Class, item.Subclass, item.MainGroup, item.Subgroup);
            item.Version = ReadFirstText(element, "ipc-version-indicator");
            item.ClassificationLevel = ReadFirstText(element, "classification-level");
            item.PositionCode = ReadFirstText(element, "symbol-position");
            item.ValueCode = ReadFirstText(element, "classification-value");
            ApplyIpcSequence(item, ipc.Count + 1);
            ipc.Add(item);
        }

        result.Ipc = ipc.ToArray();
        PatentXmlElement? national = FindDirectChild(bibliographic, "classification-national");
        if (national != null)
        {
            PatentUspcClassification item = new PatentUspcClassification();
            item.Symbol = ReadFirstText(national, "main-classification");
            item.Class = item.Symbol;
            item.IsPrimary = true;
            item.HasIsPrimary = true;
            result.Uspc = new PatentUspcClassification[] { item };
        }

        global::System.Collections.Generic.List<PatentXmlElement> wipoElements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(bibliographic, "classification-wipo", wipoElements);
        global::System.Collections.Generic.List<PatentWipoClassification> wipo = new global::System.Collections.Generic.List<PatentWipoClassification>();
        foreach (PatentXmlElement element in wipoElements)
        {
            PatentWipoClassification item = new PatentWipoClassification();
            item.FieldCode = ReadFirstText(element, "field-code");
            item.FieldName = ReadFirstText(element, "field-name");
            item.Sector = ReadFirstText(element, "sector");
            item.Sequence = wipo.Count + 1;
            item.HasSequence = true;
            wipo.Add(item);
        }

        result.Wipo = wipo.ToArray();

        return result;
    }

    internal static void ParseCpcContainer(PatentXmlElement? container, bool isPrimary, global::System.Collections.Generic.List<PatentCpcClassification> results)
    {
        if (container == null)
        {
            return;
        }

        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(container, "classification-cpc", elements);
        foreach (PatentXmlElement element in elements)
        {
            PatentCpcClassification item = new PatentCpcClassification();
            item.Section = ReadFirstText(element, "section");
            item.Class = ReadFirstText(element, "class");
            item.Subclass = ReadFirstText(element, "subclass");
            item.MainGroup = ReadFirstText(element, "main-group");
            item.Subgroup = ReadFirstText(element, "subgroup");
            item.Symbol = BuildClassificationSymbol(item.Section, item.Class, item.Subclass, item.MainGroup, item.Subgroup);
            item.Version = ReadFirstText(element, "cpc-version-indicator");
            item.IsInventive = "I".Equals(ReadFirstText(element, "classification-value"), global::System.StringComparison.OrdinalIgnoreCase);
            item.HasIsInventive = true;
            item.IsPrimary = isPrimary;
            item.HasIsPrimary = true;
            item.Sequence = results.Count + 1;
            item.HasSequence = true;
            results.Add(item);
        }
    }

    internal static PatentParty ParseParties(PatentXmlElement bibliographic)
    {
        PatentParty result = new PatentParty();
        result.Applicants = ParseApplicants(bibliographic);
        result.Inventors = ParseInventors(bibliographic);
        result.Assignees = ParseAssignees(bibliographic);
        result.Examiners = ParseExaminers(bibliographic);
        result.Attorneys = ParseAgents(bibliographic);
        return result;
    }

    internal static PatentApplicant[] ParseApplicants(PatentXmlElement root)
    {
        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(root, "us-applicant", elements);
        CollectNamedDescendants(root, "applicant", elements);
        global::System.Collections.Generic.List<PatentApplicant> results = new global::System.Collections.Generic.List<PatentApplicant>();
        foreach (PatentXmlElement element in elements)
        {
            PatentApplicant item = new PatentApplicant();
            PatentXmlElement? addressBook = FindFirstDescendant(element, "addressbook");
            item.Person = ParseOptionalPerson(addressBook);
            item.Organization = ParseOptionalOrganization(addressBook);
            item.Addresses = ParseAddresses(element);
            item.ResidenceCountryCode = ReadFirstText(FindFirstDescendant(element, "residence"), "country");
            item.ApplicantCategory = ReadAttribute(element, "app-type");
            item.Authority = ReadAttribute(element, "designation");
            ApplyApplicantSequence(item, ReadAttribute(element, "sequence"));
            results.Add(item);
        }

        return results.ToArray();
    }

    internal static PatentInventor[] ParseInventors(PatentXmlElement root)
    {
        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(root, "inventor", elements);
        global::System.Collections.Generic.List<PatentInventor> results = new global::System.Collections.Generic.List<PatentInventor>();
        foreach (PatentXmlElement element in elements)
        {
            PatentInventor item = new PatentInventor();
            PatentXmlElement? addressBook = FindFirstDescendant(element, "addressbook");
            PatentPerson? person = ParseOptionalPerson(addressBook);
            if (person != null)
            {
                item.Person = person;
            }

            item.Addresses = ParseAddresses(element);
            item.ResidenceCountryCode = ReadFirstText(FindFirstDescendant(element, "residence"), "country");
            ApplyInventorSequence(item, ReadAttribute(element, "sequence"));
            results.Add(item);
        }

        return results.ToArray();
    }

    internal static PatentAssignee[] ParseAssignees(PatentXmlElement root)
    {
        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(root, "assignee", elements);
        global::System.Collections.Generic.List<PatentAssignee> results = new global::System.Collections.Generic.List<PatentAssignee>();
        foreach (PatentXmlElement element in elements)
        {
            PatentAssignee item = new PatentAssignee();
            PatentXmlElement? addressBook = FindFirstDescendant(element, "addressbook");
            item.Person = ParseOptionalPerson(addressBook);
            item.Organization = ParseOptionalOrganization(addressBook);
            item.Addresses = ParseAddresses(element);
            item.OwnershipText = ReadFirstText(element, "role");
            ApplyAssigneeSequence(item, ReadAttribute(element, "sequence"));
            results.Add(item);
        }

        return results.ToArray();
    }

    internal static PatentExaminer[] ParseExaminers(PatentXmlElement root)
    {
        global::System.Collections.Generic.List<PatentExaminer> results = new global::System.Collections.Generic.List<PatentExaminer>();
        PatentXmlElement? primary = FindFirstDescendant(root, "primary-examiner");
        PatentXmlElement? assistant = FindFirstDescendant(root, "assistant-examiner");
        if (primary != null)
        {
            results.Add(ParseExaminer(primary, true));
        }

        if (assistant != null)
        {
            results.Add(ParseExaminer(assistant, false));
        }

        return results.ToArray();
    }

    internal static PatentExaminer ParseExaminer(PatentXmlElement element, bool isPrimary)
    {
        PatentExaminer result = new PatentExaminer();
        PatentPerson? person = ParseOptionalPerson(element);
        if (person != null)
        {
            result.Person = person;
        }

        result.ArtUnit = ReadFirstText(element, "department");
        result.IsPrimary = isPrimary;
        result.HasIsPrimary = true;
        return result;
    }

    internal static PatentAttorney[] ParseAgents(PatentXmlElement root)
    {
        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(root, "agent", elements);
        global::System.Collections.Generic.List<PatentAttorney> results = new global::System.Collections.Generic.List<PatentAttorney>();
        foreach (PatentXmlElement element in elements)
        {
            PatentAttorney item = new PatentAttorney();
            PatentXmlElement? addressBook = FindFirstDescendant(element, "addressbook");
            PatentPerson? person = ParseOptionalPerson(addressBook);
            if (person != null)
            {
                item.Person = person;
            }

            item.Organization = ParseOptionalOrganization(addressBook);
            item.Addresses = ParseAddresses(element);
            item.PractitionerCategory = ReadAttribute(element, "rep-type");
            results.Add(item);
        }

        return results.ToArray();
    }

    internal static PatentPerson? ParseOptionalPerson(PatentXmlElement? element)
    {
        if (element == null)
        {
            return null;
        }

        string? first = ReadFirstText(element, "first-name");
        string? middle = ReadFirstText(element, "middle-name");
        string? last = ReadFirstText(element, "last-name");
        string? full = ReadFirstText(element, "name");
        if (first == null && middle == null && last == null && full == null)
        {
            return null;
        }

        PatentPerson result = new PatentPerson();
        result.FirstName = first;
        result.MiddleName = middle;
        result.LastName = last;
        result.FullName = full;
        if (result.FullName == null)
        {
            result.FullName = BuildPersonName(first, middle, last);
        }

        return result;
    }

    internal static PatentOrganization? ParseOptionalOrganization(PatentXmlElement? element)
    {
        string? name = ReadFirstText(element, "orgname");
        if (name == null)
        {
            return null;
        }

        PatentOrganization result = new PatentOrganization();
        result.Name = name;
        return result;
    }

    internal static PatentAddress[] ParseAddresses(PatentXmlElement element)
    {
        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(element, "address", elements);
        global::System.Collections.Generic.List<PatentAddress> results = new global::System.Collections.Generic.List<PatentAddress>();
        foreach (PatentXmlElement address in elements)
        {
            PatentAddress item = new PatentAddress();
            item.AddressLineOne = ReadFirstText(address, "address-1");
            item.AddressLineTwo = ReadFirstText(address, "address-2");
            item.City = ReadFirstText(address, "city");
            item.GeographicRegionCode = ReadFirstText(address, "state");
            item.PostalCode = ReadFirstText(address, "postcode");
            item.CountryCode = ReadFirstText(address, "country");
            results.Add(item);
        }

        return results.ToArray();
    }

    internal static void ApplyGovernmentInterests(PatentClassificationData classifications, PatentXmlElement bibliographic)
    {
        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(bibliographic, "us-gov-interest", elements);
        CollectNamedDescendants(bibliographic, "government-interest", elements);
        global::System.Collections.Generic.List<PatentGovernmentInterest> results = new global::System.Collections.Generic.List<PatentGovernmentInterest>();
        foreach (PatentXmlElement element in elements)
        {
            PatentGovernmentInterest item = new PatentGovernmentInterest();
            item.Statement = ReadNormalizedText(element);
            item.AgencyName = ReadFirstText(element, "agency-name");
            item.AgencyIdentifier = ReadFirstText(element, "agency-code");
            item.AwardNumber = ReadFirstText(element, "contract-number");
            item.RightsCategory = ReadFirstText(element, "rights-category");
            results.Add(item);
        }

        classifications.GovernmentInterests = results.ToArray();
    }

    internal static PatentExternalResource[] ParseExternalResources(PatentXmlElement root)
    {
        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectExternalResourceDescendants(root, elements);
        global::System.Collections.Generic.List<PatentExternalResource> results = new global::System.Collections.Generic.List<PatentExternalResource>();
        foreach (PatentXmlElement element in elements)
        {
            PatentExternalResource item = new PatentExternalResource();
            item.Identifier = ReadAttribute(element, "id");
            item.FileName = ReadAttribute(element, "file");
            if (item.FileName == null)
            {
                item.FileName = ReadAttribute(element, "href");
            }

            item.Format = ReadAttribute(element, "img-format");
            item.ContentCategory = ReadAttribute(element, "img-content");
            item.AlternativeText = ReadAttribute(element, "alt");
            item.Orientation = ReadAttribute(element, "orientation");
            item.Width = ReadAttribute(element, "wi");
            item.Height = ReadAttribute(element, "he");
            PatentXmlElement? figure = FindContainingFigure(root, element);
            if (figure != null)
            {
                item.FigureIdentifier = ReadAttribute(figure, "id");
                item.FigureNumber = ReadAttribute(figure, "num");
            }

            results.Add(item);
        }

        return results.ToArray();
    }

    internal static void CollectExternalResourceDescendants(PatentXmlElement element, global::System.Collections.Generic.List<PatentXmlElement> results)
    {
        foreach (PatentXmlElement child in element.Children)
        {
            if (child.Name.Equals("img", global::System.StringComparison.Ordinal) || child.Name.Equals("external-resource", global::System.StringComparison.Ordinal))
            {
                results.Add(child);
            }

            CollectExternalResourceDescendants(child, results);
        }
    }

    internal static PatentXmlElement? FindContainingFigure(PatentXmlElement root, PatentXmlElement target)
    {
        foreach (PatentXmlElement child in root.Children)
        {
            if (child.Name.Equals("figure", global::System.StringComparison.Ordinal) && ContainsElement(child, target))
            {
                return child;
            }

            PatentXmlElement? found = FindContainingFigure(child, target);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    internal static bool ContainsElement(PatentXmlElement root, PatentXmlElement target)
    {
        foreach (PatentXmlElement child in root.Children)
        {
            if (global::System.Object.ReferenceEquals(child, target) || ContainsElement(child, target))
            {
                return true;
            }
        }

        return false;
    }

    internal static PatentXmlElement? FindFirstDescendant(PatentXmlElement element, string name)
    {
        foreach (PatentXmlElement child in element.Children)
        {
            if (child.Name.Equals(name, global::System.StringComparison.Ordinal))
            {
                return child;
            }

            PatentXmlElement? found = FindFirstDescendant(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    internal static PatentXmlElement? FindDirectChild(PatentXmlElement element, string name)
    {
        foreach (PatentXmlElement child in element.Children)
        {
            if (child.Name.Equals(name, global::System.StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    internal static void CollectNamedDescendants(PatentXmlElement element, string name, global::System.Collections.Generic.List<PatentXmlElement> results)
    {
        foreach (PatentXmlElement child in element.Children)
        {
            if (child.Name.Equals(name, global::System.StringComparison.Ordinal))
            {
                results.Add(child);
            }

            CollectNamedDescendants(child, name, results);
        }
    }

    internal static void CollectTextsByName(PatentXmlElement element, string name, global::System.Collections.Generic.List<string> results)
    {
        global::System.Collections.Generic.List<PatentXmlElement> elements = new global::System.Collections.Generic.List<PatentXmlElement>();
        CollectNamedDescendants(element, name, elements);
        foreach (PatentXmlElement child in elements)
        {
            string text = ReadNormalizedText(child);
            if (text.Length > 0)
            {
                results.Add(text);
            }
        }
    }

    internal static string? ReadFirstText(PatentXmlElement? element, string name)
    {
        if (element == null)
        {
            return null;
        }

        PatentXmlElement? child = FindFirstDescendant(element, name);
        if (child == null)
        {
            return null;
        }

        string text = ReadNormalizedText(child);
        return text.Length == 0 ? null : text;
    }

    internal static string ReadNormalizedText(PatentXmlElement element)
    {
        global::System.Text.StringBuilder flattened = new global::System.Text.StringBuilder();
        AppendFlattenedText(element, flattened);
        return CollapseWhitespace(flattened.ToString());
    }

    internal static string ReadNormalizedClaimText(PatentXmlElement element)
    {
        global::System.Text.StringBuilder flattened = new global::System.Text.StringBuilder();
        foreach (PatentXmlContent content in element.Content)
        {
            if (content.Text != null)
            {
                flattened.Append(content.Text);
            }
            else if (content.Element != null && !content.Element.Name.Equals("claim-number", global::System.StringComparison.Ordinal))
            {
                bool boundary = IsTextBoundaryElement(content.Element.Name);
                if (boundary)
                {
                    flattened.Append(' ');
                }

                AppendFlattenedText(content.Element, flattened);
                if (boundary)
                {
                    flattened.Append(' ');
                }
            }
        }

        return CollapseWhitespace(flattened.ToString());
    }

    internal static void AppendFlattenedText(PatentXmlElement element, global::System.Text.StringBuilder result)
    {
        foreach (PatentXmlContent content in element.Content)
        {
            if (content.Text != null)
            {
                result.Append(content.Text);
            }
            else if (content.Element != null)
            {
                bool boundary = IsTextBoundaryElement(content.Element.Name);
                if (boundary)
                {
                    result.Append(' ');
                }

                AppendFlattenedText(content.Element, result);
                if (boundary)
                {
                    result.Append(' ');
                }
            }
        }
    }

    internal static bool IsTextBoundaryElement(string name)
    {
        if (name.Equals("claim-text", global::System.StringComparison.Ordinal) || name.Equals("p", global::System.StringComparison.Ordinal) || name.Equals("heading", global::System.StringComparison.Ordinal))
        {
            return true;
        }

        if (name.Equals("br", global::System.StringComparison.Ordinal) || name.Equals("li", global::System.StringComparison.Ordinal) || name.Equals("dt", global::System.StringComparison.Ordinal) || name.Equals("dd", global::System.StringComparison.Ordinal))
        {
            return true;
        }

        return name.Equals("row", global::System.StringComparison.Ordinal) || name.Equals("tr", global::System.StringComparison.Ordinal) || name.Equals("entry", global::System.StringComparison.Ordinal) || name.Equals("td", global::System.StringComparison.Ordinal);
    }

    internal static string CollapseWhitespace(string value)
    {
        global::System.Text.StringBuilder result = new global::System.Text.StringBuilder();
        bool pendingSpace = false;
        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (global::System.Char.IsWhiteSpace(character))
            {
                if (result.Length > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    result.Append(' ');
                    pendingSpace = false;
                }

                result.Append(character);
            }
        }

        return result.ToString();
    }

    internal static string? ReadAttribute(PatentXmlElement element, string name)
    {
        if (!element.Attributes.ContainsKey(name))
        {
            return null;
        }

        string value = element.Attributes[name].Trim();
        return value.Length == 0 ? null : value;
    }

    internal static string BuildClassificationSymbol(string? section, string? classCode, string? subclass, string? mainGroup, string? subgroup)
    {
        string prefix = (section == null ? string.Empty : section) + (classCode == null ? string.Empty : classCode) + (subclass == null ? string.Empty : subclass);
        string group = mainGroup == null ? string.Empty : mainGroup;
        if (subgroup != null)
        {
            group += "/" + subgroup;
        }

        string value = (prefix + " " + group).Trim();
        return value;
    }

    internal static string? BuildPersonName(string? first, string? middle, string? last)
    {
        string value = string.Empty;
        if (first != null)
        {
            value = first;
        }

        if (middle != null)
        {
            value = (value + " " + middle).Trim();
        }

        if (last != null)
        {
            value = (value + " " + last).Trim();
        }

        return value.Length == 0 ? null : value;
    }

    internal static void ApplyRootDates(PatentFullText result)
    {
        JsonDateParseResult produced = JsonFieldReader.ParseFlexibleDate(result.ProducedDateText == null ? string.Empty : result.ProducedDateText);
        if (produced.IsValid)
        {
            result.ProducedDate = produced.Value;
            result.HasProducedDate = true;
        }

        JsonDateParseResult published = JsonFieldReader.ParseFlexibleDate(result.PublishedDateText == null ? string.Empty : result.PublishedDateText);
        if (published.IsValid)
        {
            result.PublishedDate = published.Value;
            result.HasPublishedDate = true;
        }
    }

    internal static void ApplyClaimCount(PatentBibliographicData result, string? value)
    {
        JsonIntegerParseResult parsed = JsonFieldReader.ParseSignedInteger(value == null ? string.Empty : value);
        if (parsed.IsValid && parsed.Value >= 0L && parsed.Value <= global::System.Int32.MaxValue)
        {
            result.ClaimCount = (int)parsed.Value;
            result.HasClaimCount = true;
        }
    }

    internal static void ApplyApplicantSequence(PatentApplicant result, string? value)
    {
        JsonIntegerParseResult parsed = JsonFieldReader.ParseSignedInteger(value == null ? string.Empty : value);
        if (parsed.IsValid && parsed.Value >= 0L && parsed.Value <= global::System.Int32.MaxValue)
        {
            result.Sequence = (int)parsed.Value;
            result.HasSequence = true;
        }
    }

    internal static void ApplyInventorSequence(PatentInventor result, string? value)
    {
        JsonIntegerParseResult parsed = JsonFieldReader.ParseSignedInteger(value == null ? string.Empty : value);
        if (parsed.IsValid && parsed.Value >= 0L && parsed.Value <= global::System.Int32.MaxValue)
        {
            result.Sequence = (int)parsed.Value;
            result.HasSequence = true;
        }
    }

    internal static void ApplyAssigneeSequence(PatentAssignee result, string? value)
    {
        JsonIntegerParseResult parsed = JsonFieldReader.ParseSignedInteger(value == null ? string.Empty : value);
        if (parsed.IsValid && parsed.Value >= 0L && parsed.Value <= global::System.Int32.MaxValue)
        {
            result.Sequence = (int)parsed.Value;
            result.HasSequence = true;
        }
    }

    internal static void ApplyIpcSequence(PatentIpcClassification result, int sequence)
    {
        result.Sequence = sequence;
        result.HasSequence = true;
    }

    internal static ParseException ParseFailure(string message, string? sourceName, string path)
    {
        return new ParseException(message, DocumentFormat.Xml, sourceName, path, 0L, false, 0, false, null);
    }
}
