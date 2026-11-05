using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace fix;

internal static class OrchestraXmlEmitter
{
    internal const string DublinCoreTermsNamespace = "http://purl.org/dc/terms/";
    internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    internal static OrchestraReadResult WriteRepository(Stream destination, OrchestraRepository repository, OrchestraWriteOptions options)
    {
        OrchestraReadResult result = ValidateInputs(destination, repository, options);
        if (!result.Success) return result;
        Encoding? encoding = ResolveEncoding(options.EncodingName, result.Diagnostics);
        if (encoding == null)
        {
            result.Success = false;
            return result;
        }

        bool interfacesDocument = IsInterfacesDocument(repository, options);
        if (!interfacesDocument && options.IncludeInterfaces && repository.Interfaces.Count != 0)
        {
            AddDiagnostic(result.Diagnostics, ValidationSeverity.Warning, "interfaces-not-written", "Interface content requires a separate interfaces document and was not written with repository definitions.", "/interfaces");
        }
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Encoding = encoding;
        settings.Indent = options.Indent;
        settings.OmitXmlDeclaration = false;
        settings.CloseOutput = false;
        settings.NewLineChars = "\n";
        settings.NewLineHandling = NewLineHandling.Replace;
        using (XmlWriter writer = XmlWriter.Create(destination, settings))
        {
            writer.WriteStartDocument();
            if (interfacesDocument) OrchestraInterfacesEmitter.WriteInterfacesDocument(writer, repository, options);
            else WriteRepositoryDocument(writer, repository, options);
            writer.WriteEndDocument();
        }
        result.Success = !HasErrors(result.Diagnostics);
        return result;
    }

    internal static OrchestraReadResult ValidateInputs(Stream destination, OrchestraRepository repository, OrchestraWriteOptions options)
    {
        OrchestraReadResult result = new OrchestraReadResult();
        result.Repository = repository;
        result.Success = true;
        if (destination == null || !destination.CanWrite) AddDiagnostic(result.Diagnostics, ValidationSeverity.Error, "unwritable-orchestra-destination", "The Orchestra XML destination is not writable.", "/");
        if (repository == null) AddDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-orchestra-repository", "The Orchestra repository is missing.", "/");
        if (options == null) AddDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-orchestra-write-options", "The Orchestra write options are missing.", "/");
        else if (options.Version != OrchestraVersion.Version10 && options.Version != OrchestraVersion.Version11) AddDiagnostic(result.Diagnostics, ValidationSeverity.Error, "unsupported-orchestra-write-version", "The target Orchestra version is not supported.", "/");
        result.Success = !HasErrors(result.Diagnostics);
        return result;
    }

    internal static Encoding? ResolveEncoding(string name, List<OrchestraDiagnostic> diagnostics)
    {
        string normalized = name == null ? "" : name.Trim().ToLowerInvariant();
        if (normalized == "utf-8" || normalized == "utf8") return new UTF8Encoding(false);
        if (normalized == "utf-16" || normalized == "utf-16le" || normalized == "unicode") return new UnicodeEncoding(false, false);
        if (normalized == "utf-16be" || normalized == "unicodefffe") return new UnicodeEncoding(true, false);
        if (normalized == "us-ascii" || normalized == "ascii") return Encoding.ASCII;
        AddDiagnostic(diagnostics, ValidationSeverity.Error, "unsupported-orchestra-encoding", $"The output encoding '{name}' is not supported.", "/");
        return null;
    }

    internal static bool IsInterfacesDocument(OrchestraRepository repository, OrchestraWriteOptions options)
    {
        if (!options.IncludeInterfaces || repository.Interfaces.Count == 0) return false;
        return !HasRepositoryContent(repository, options);
    }

    internal static bool HasRepositoryContent(OrchestraRepository repository, OrchestraWriteOptions options)
    {
        if (repository.Categories.Count != 0 || repository.Sections.Count != 0 || repository.Concepts.Count != 0) return true;
        if (repository.EncodingStandards.Count != 0 || repository.Datatypes.Count != 0 || repository.CodeSets.Count != 0 || repository.Fields.Count != 0) return true;
        if (repository.Components.Count != 0 || repository.Groups.Count != 0 || repository.Messages.Count != 0 || repository.Scenarios.Count != 0) return true;
        if (options.IncludeBehavior && (repository.Actors.Count != 0 || repository.Flows.Count != 0 || repository.StateMachines.Count != 0 || repository.Rules.Count != 0)) return true;
        return false;
    }

    internal static void WriteRepositoryDocument(XmlWriter writer, OrchestraRepository repository, OrchestraWriteOptions options)
    {
        string namespaceUri = RepositoryNamespace(options.Version);
        writer.WriteStartElement("fixr", "repository", namespaceUri);
        writer.WriteAttributeString("xmlns", "dcterms", null, DublinCoreTermsNamespace);
        WriteOptionalAttribute(writer, "name", repository.Metadata.Name);
        string release = repository.Metadata.Release.Length == 0 ? repository.Metadata.FixVersion : repository.Metadata.Release;
        WriteOptionalAttribute(writer, "version", release);
        WriteOptionalAttribute(writer, "namespace", repository.Metadata.RepositoryNamespace);
        WriteOptionalAttribute(writer, "specUrl", repository.Metadata.SpecificationUri);
        WriteExtensionAttributes(writer, repository.Extensions);
        WriteMetadata(writer, repository.Metadata, namespaceUri);
        WriteCategories(writer, repository.Categories, options, namespaceUri);
        WriteSections(writer, repository.Sections, options, namespaceUri);
        WriteConcepts(writer, repository.Concepts, options, namespaceUri);
        if (options.Version == OrchestraVersion.Version11) WriteEncodingStandards(writer, repository.EncodingStandards, options, namespaceUri);
        WriteDatatypes(writer, repository.Datatypes, options, namespaceUri);
        WriteCodeSets(writer, repository, options, namespaceUri);
        WriteFields(writer, repository.Fields, options, namespaceUri);
        WriteComponents(writer, repository, options, namespaceUri);
        WriteGroups(writer, repository, options, namespaceUri);
        WriteMessages(writer, repository, options, namespaceUri);
        if (options.Version == OrchestraVersion.Version11) WriteScenarios(writer, repository.Scenarios, options, namespaceUri);
        if (options.IncludeBehavior) OrchestraBehaviorEmitter.WriteActorsAndFlows(writer, repository, options, namespaceUri);
        WriteExtensions(writer, repository.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteMetadata(XmlWriter writer, OrchestraMetadata metadata, string namespaceUri)
    {
        if (metadata.Terms.Count == 0 && metadata.PublicationDate.Length == 0) return;
        string orchestraPrefix = NamespacePrefix(namespaceUri);
        writer.WriteStartElement(orchestraPrefix, "metadata", namespaceUri);
        for (int index = 0; index < metadata.Terms.Count; index++)
        {
            OrchestraMetadataTerm term = metadata.Terms[index];
            if (term.Name.Length == 0 && term.Xml.Length != 0)
            {
                WriteXmlFragment(writer, term.Xml);
            }
            else
            {
                string termNamespace = term.NamespaceUri.Length == 0 ? DublinCoreTermsNamespace : term.NamespaceUri;
                string prefix = termNamespace == DublinCoreTermsNamespace ? "dcterms" : "meta";
                writer.WriteStartElement(prefix, term.Name, termNamespace);
                if (term.Language.Length != 0) writer.WriteAttributeString("xml", "lang", XmlNamespace, term.Language);
                writer.WriteString(term.Value);
                writer.WriteEndElement();
            }
        }
        if (metadata.Terms.Count == 0 && metadata.PublicationDate.Length != 0)
        {
            writer.WriteElementString("dcterms", "issued", DublinCoreTermsNamespace, metadata.PublicationDate);
        }
        writer.WriteEndElement();
    }

    internal static void WriteCategories(XmlWriter writer, List<OrchestraCategory> source, OrchestraWriteOptions options, string namespaceUri)
    {
        if (source.Count == 0) return;
        List<OrchestraCategory> items = new List<OrchestraCategory>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareNames(left.Name, right.Name));
        writer.WriteStartElement("fixr", "categories", namespaceUri);
        for (int index = 0; index < items.Count; index++)
        {
            OrchestraCategory item = items[index];
            writer.WriteStartElement("fixr", "category", namespaceUri);
            WriteOptionalAttribute(writer, "name", item.Name);
            WriteOptionalAttribute(writer, "section", item.Section);
            WriteOptionalAttribute(writer, "FIXMLFileName", item.FixmlFileName);
            WriteOptionalAttribute(writer, "componentType", item.ComponentType);
            WriteOptionalAttribute(writer, "includeFile", item.IncludeFile);
            WritePedigree(writer, item.Pedigree);
            WriteExtensionAttributes(writer, item.Extensions);
            WriteAnnotations(writer, item.Annotations, namespaceUri);
            WriteEncodings(writer, item.Encodings, namespaceUri);
            WriteExtensions(writer, item.Extensions);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteSections(XmlWriter writer, List<OrchestraSection> source, OrchestraWriteOptions options, string namespaceUri)
    {
        if (source.Count == 0) return;
        List<OrchestraSection> items = new List<OrchestraSection>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareNames(left.Name, right.Name));
        writer.WriteStartElement("fixr", "sections", namespaceUri);
        for (int index = 0; index < items.Count; index++)
        {
            OrchestraSection item = items[index];
            writer.WriteStartElement("fixr", "section", namespaceUri);
            WriteOptionalAttribute(writer, "name", item.Name);
            string displayOrder = item.DisplayOrderText;
            if (displayOrder.Length == 0 && item.HasDisplayOrder) displayOrder = item.DisplayOrder.ToString(System.Globalization.CultureInfo.InvariantCulture);
            WriteOptionalAttribute(writer, "displayOrder", displayOrder);
            WriteOptionalAttribute(writer, "FIXMLFileName", item.FixmlFileName);
            WritePedigree(writer, item.Pedigree);
            WriteExtensionAttributes(writer, item.Extensions);
            WriteAnnotations(writer, item.Annotations, namespaceUri);
            WriteEncodings(writer, item.Encodings, namespaceUri);
            WriteExtensions(writer, item.Extensions);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteConcepts(XmlWriter writer, List<OrchestraConcept> source, OrchestraWriteOptions options, string namespaceUri)
    {
        if (source.Count == 0) return;
        List<OrchestraConcept> items = new List<OrchestraConcept>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareNames(left.Name, right.Name));
        writer.WriteStartElement("fixr", "concepts", namespaceUri);
        for (int index = 0; index < items.Count; index++)
        {
            OrchestraConcept item = items[index];
            writer.WriteStartElement("fixr", "concept", namespaceUri);
            WriteOptionalAttribute(writer, "name", item.Name);
            WriteExtensionAttributes(writer, item.Extensions);
            for (int memberIndex = 0; memberIndex < item.Members.Count; memberIndex++) WriteConceptMember(writer, item.Members[memberIndex], namespaceUri);
            WriteAnnotations(writer, item.Annotations, namespaceUri);
            WriteExtensions(writer, item.Extensions);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteConceptMember(XmlWriter writer, OrchestraConceptMember member, string namespaceUri)
    {
        string kind = member.Kind.Length == 0 ? "fieldRef" : member.Kind;
        writer.WriteStartElement("fixr", kind, namespaceUri);
        WriteOptionalAttribute(writer, "id", member.Id);
        WriteOptionalAttribute(writer, "name", member.Name);
        WriteScenarioAttribute(writer, member.Scenario);
        WriteOptionalAttribute(writer, "msgType", member.MessageType);
        WritePedigree(writer, member.Pedigree);
        WriteExtensionAttributes(writer, member.Extensions);
        WriteAnnotations(writer, member.Annotations, namespaceUri);
        WriteExtensions(writer, member.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteEncodingStandards(XmlWriter writer, List<OrchestraEncodingStandard> source, OrchestraWriteOptions options, string namespaceUri)
    {
        if (source.Count == 0) return;
        List<OrchestraEncodingStandard> items = new List<OrchestraEncodingStandard>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareNames(left.Name, right.Name));
        writer.WriteStartElement("fixr", "encodingStandards", namespaceUri);
        for (int index = 0; index < items.Count; index++)
        {
            OrchestraEncodingStandard item = items[index];
            writer.WriteStartElement("fixr", "encodingStandard", namespaceUri);
            WriteOptionalAttribute(writer, "name", item.Name);
            WriteOptionalAttribute(writer, "displayName", item.DisplayName);
            WriteExtensionAttributes(writer, item.Extensions);
            if (item.Encoding != null) WriteEncoding(writer, item.Encoding, namespaceUri);
            WriteAnnotations(writer, item.Annotations, namespaceUri);
            WriteExtensions(writer, item.Extensions);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteDatatypes(XmlWriter writer, List<OrchestraDatatypeDefinition> source, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "datatypes", namespaceUri);
        List<OrchestraDatatypeDefinition> items = new List<OrchestraDatatypeDefinition>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareNames(left.Name, right.Name));
        for (int index = 0; index < items.Count; index++)
        {
            OrchestraDatatypeDefinition item = items[index];
            writer.WriteStartElement("fixr", "datatype", namespaceUri);
            WriteOptionalAttribute(writer, "name", item.Name);
            WriteOptionalAttribute(writer, "baseType", item.BaseType);
            WritePedigree(writer, item.Pedigree);
            WriteExtensionAttributes(writer, item.Extensions);
            for (int mappingIndex = 0; mappingIndex < item.Mappings.Count; mappingIndex++) WriteDatatypeMapping(writer, item.Mappings[mappingIndex], namespaceUri);
            WriteAnnotations(writer, item.Annotations, namespaceUri);
            WriteExtensions(writer, item.Extensions);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteDatatypeMapping(XmlWriter writer, OrchestraDatatypeMapping mapping, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "mappedDatatype", namespaceUri);
        WriteOptionalAttribute(writer, "standard", mapping.Standard);
        string builtin = mapping.BuiltinText;
        if (builtin.Length == 0 && mapping.HasBuiltin) builtin = mapping.Builtin ? "true" : "false";
        WriteOptionalAttribute(writer, "builtin", builtin);
        WriteOptionalAttribute(writer, "base", mapping.Base);
        WriteOptionalAttribute(writer, "pattern", mapping.Pattern);
        WriteOptionalAttribute(writer, "element", mapping.Element);
        WriteOptionalAttribute(writer, "size", mapping.Size);
        WriteOptionalAttribute(writer, "parameter", mapping.Parameter);
        WriteOptionalAttribute(writer, "minInclusive", mapping.MinimumInclusive);
        WriteOptionalAttribute(writer, "maxInclusive", mapping.MaximumInclusive);
        WriteExtensionAttributes(writer, mapping.Extensions);
        if (mapping.Encoding != null) WriteEncoding(writer, mapping.Encoding, namespaceUri);
        WriteAnnotations(writer, mapping.Annotations, namespaceUri);
        WriteExtensions(writer, mapping.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteCodeSets(XmlWriter writer, OrchestraRepository repository, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "codeSets", namespaceUri);
        List<OrchestraCodeSetDefinition> items = new List<OrchestraCodeSetDefinition>(repository.CodeSets);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareDefinitions(left.Name, left.Scenario, left.Id, right.Name, right.Scenario, right.Id));
        for (int index = 0; index < items.Count; index++) WriteCodeSet(writer, items[index], options, namespaceUri);
        WriteEncodings(writer, repository.CodeSetEncodings, namespaceUri);
        writer.WriteEndElement();
    }

    internal static void WriteCodeSet(XmlWriter writer, OrchestraCodeSetDefinition item, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "codeSet", namespaceUri);
        WriteOptionalAttribute(writer, "id", item.Id);
        WriteOptionalAttribute(writer, "name", item.Name);
        WriteScenarioAttribute(writer, item.Scenario);
        WriteOptionalAttribute(writer, "type", item.Type);
        WriteOptionalAttribute(writer, "default", item.DefaultValue);
        WriteOptionalAttribute(writer, "specUrl", item.SpecificationUri);
        WriteOptionalAttribute(writer, "unionDataType", item.UnionDatatype);
        WritePedigree(writer, item.Pedigree);
        WriteExtensionAttributes(writer, item.Extensions);
        List<OrchestraCodeDefinition> codes = new List<OrchestraCodeDefinition>(item.Codes);
        if (options.CanonicalOrder) codes.Sort((left, right) => CompareDefinitions(left.Value, left.Scenario, left.Id, right.Value, right.Scenario, right.Id));
        for (int index = 0; index < codes.Count; index++) WriteCode(writer, codes[index], namespaceUri);
        WriteAnnotations(writer, item.Annotations, namespaceUri);
        WriteEncodings(writer, item.Encodings, namespaceUri);
        WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteCode(XmlWriter writer, OrchestraCodeDefinition item, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "code", namespaceUri);
        WriteOptionalAttribute(writer, "id", item.Id);
        WriteOptionalAttribute(writer, "name", item.Name);
        WriteScenarioAttribute(writer, item.Scenario);
        WriteOptionalAttribute(writer, "value", item.Value);
        WriteOptionalAttribute(writer, "sort", item.Sort);
        WriteOptionalAttribute(writer, "group", item.Group);
        WritePedigree(writer, item.Pedigree);
        WriteExtensionAttributes(writer, item.Extensions);
        WriteAnnotations(writer, item.Annotations, namespaceUri);
        WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteFields(XmlWriter writer, List<OrchestraFieldDefinition> source, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "fields", namespaceUri);
        List<OrchestraFieldDefinition> items = new List<OrchestraFieldDefinition>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareDefinitions(left.Name, left.Scenario, left.Id, right.Name, right.Scenario, right.Id));
        for (int index = 0; index < items.Count; index++) WriteField(writer, items[index], options, namespaceUri);
        writer.WriteEndElement();
    }

    internal static void WriteField(XmlWriter writer, OrchestraFieldDefinition item, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "field", namespaceUri);
        WriteOptionalAttribute(writer, "id", item.Id);
        WriteOptionalAttribute(writer, "name", item.Name);
        WriteOptionalAttribute(writer, "abbrName", item.Abbreviation);
        WriteScenarioAttribute(writer, item.Scenario);
        WriteOptionalAttribute(writer, "type", item.Type);
        WriteOptionalAttribute(writer, "codeSet", item.CodeSet);
        WriteOptionalAttribute(writer, "unionDataType", item.UnionDatatype);
        WritePresenceAttribute(writer, item.PresenceText, item.Presence);
        WriteOptionalAttribute(writer, "value", item.Value);
        WriteOptionalAttribute(writer, "lengthId", item.LengthId);
        WriteOptionalAttribute(writer, "discriminatorId", item.DiscriminatorId);
        WritePedigree(writer, item.Pedigree);
        WriteExtensionAttributes(writer, item.Extensions);
        if (options.IncludeBehavior) OrchestraBehaviorEmitter.WriteRules(writer, item.Rules, namespaceUri);
        WriteAnnotations(writer, item.Annotations, namespaceUri);
        WriteEncodings(writer, item.Encodings, namespaceUri);
        WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteComponents(XmlWriter writer, OrchestraRepository repository, OrchestraWriteOptions options, string namespaceUri)
    {
        if (repository.Components.Count == 0 && repository.ComponentEncodings.Count == 0) return;
        writer.WriteStartElement("fixr", "components", namespaceUri);
        List<OrchestraComponentDefinition> items = new List<OrchestraComponentDefinition>(repository.Components);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareDefinitions(left.Name, left.Scenario, left.Id, right.Name, right.Scenario, right.Id));
        for (int index = 0; index < items.Count; index++) WriteComponent(writer, items[index], options, namespaceUri);
        WriteEncodings(writer, repository.ComponentEncodings, namespaceUri);
        writer.WriteEndElement();
    }

    internal static void WriteComponent(XmlWriter writer, OrchestraComponentDefinition item, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "component", namespaceUri);
        WriteDefinitionAttributes(writer, item.Id, item.Name, item.Abbreviation, item.Scenario, item.Category, item.Rendering);
        WriteOptionalAttribute(writer, "which", item.Selection);
        WritePedigree(writer, item.Pedigree);
        WriteExtensionAttributes(writer, item.Extensions);
        WriteMembers(writer, item.Members, options, namespaceUri);
        WriteAnnotations(writer, item.Annotations, namespaceUri);
        WriteEncodings(writer, item.Encodings, namespaceUri);
        WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteGroups(XmlWriter writer, OrchestraRepository repository, OrchestraWriteOptions options, string namespaceUri)
    {
        if (repository.Groups.Count == 0 && repository.GroupEncodings.Count == 0) return;
        writer.WriteStartElement("fixr", "groups", namespaceUri);
        List<OrchestraGroupDefinition> items = new List<OrchestraGroupDefinition>(repository.Groups);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareDefinitions(left.Name, left.Scenario, left.Id, right.Name, right.Scenario, right.Id));
        for (int index = 0; index < items.Count; index++) WriteGroup(writer, items[index], options, namespaceUri);
        WriteEncodings(writer, repository.GroupEncodings, namespaceUri);
        writer.WriteEndElement();
    }

    internal static void WriteGroup(XmlWriter writer, OrchestraGroupDefinition item, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "group", namespaceUri);
        WriteDefinitionAttributes(writer, item.Id, item.Name, item.Abbreviation, item.Scenario, item.Category, item.Rendering);
        WriteOptionalAttribute(writer, "which", item.Selection);
        WritePedigree(writer, item.Pedigree);
        WriteExtensionAttributes(writer, item.Extensions);
        if (item.CountMember != null) WriteMember(writer, item.CountMember, options, namespaceUri, "numInGroup");
        WriteMembers(writer, item.Members, options, namespaceUri);
        WriteAnnotations(writer, item.Annotations, namespaceUri);
        WriteEncodings(writer, item.Encodings, namespaceUri);
        WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteMessages(XmlWriter writer, OrchestraRepository repository, OrchestraWriteOptions options, string namespaceUri)
    {
        if (repository.Messages.Count == 0 && repository.MessageEncodings.Count == 0) return;
        writer.WriteStartElement("fixr", "messages", namespaceUri);
        List<OrchestraMessageDefinition> items = new List<OrchestraMessageDefinition>(repository.Messages);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareDefinitions(left.Name, left.Scenario, left.Id, right.Name, right.Scenario, right.Id));
        for (int index = 0; index < items.Count; index++) WriteMessage(writer, items[index], options, namespaceUri);
        WriteEncodings(writer, repository.MessageEncodings, namespaceUri);
        writer.WriteEndElement();
    }

    internal static void WriteMessage(XmlWriter writer, OrchestraMessageDefinition item, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "message", namespaceUri);
        WriteOptionalAttribute(writer, "id", item.Id);
        WriteOptionalAttribute(writer, "name", item.Name);
        WriteOptionalAttribute(writer, "abbrName", item.Abbreviation);
        WriteScenarioAttribute(writer, item.Scenario);
        WriteOptionalAttribute(writer, "msgType", item.MessageType);
        WriteOptionalAttribute(writer, "category", item.Category);
        WriteOptionalAttribute(writer, "flow", item.Flow);
        WritePedigree(writer, item.Pedigree);
        WriteExtensionAttributes(writer, item.Extensions);
        if (item.Members.Count != 0)
        {
            writer.WriteStartElement("fixr", "structure", namespaceUri);
            WriteOptionalAttribute(writer, "which", item.Selection);
            WriteMembers(writer, item.Members, options, namespaceUri);
            writer.WriteEndElement();
        }
        if (options.IncludeBehavior) OrchestraBehaviorEmitter.WriteMessageBehavior(writer, item, namespaceUri);
        WriteAnnotations(writer, item.Annotations, namespaceUri);
        WriteEncodings(writer, item.Encodings, namespaceUri);
        WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteMembers(XmlWriter writer, List<OrchestraMemberDefinition> members, OrchestraWriteOptions options, string namespaceUri)
    {
        for (int index = 0; index < members.Count; index++) WriteMember(writer, members[index], options, namespaceUri, members[index].Kind);
    }

    internal static void WriteMember(XmlWriter writer, OrchestraMemberDefinition item, OrchestraWriteOptions options, string namespaceUri, string kind)
    {
        string elementName = kind.Length == 0 ? "fieldRef" : kind;
        writer.WriteStartElement("fixr", elementName, namespaceUri);
        WriteOptionalAttribute(writer, "id", item.ReferenceId);
        WriteScenarioAttribute(writer, item.Scenario);
        WriteOptionalAttribute(writer, "instanceName", item.InstanceName);
        WritePresenceAttribute(writer, item.PresenceText, item.Presence);
        WriteOptionalAttribute(writer, "value", item.Value);
        WriteOptionalAttribute(writer, "implMinOccurs", item.ImplementationMinimum);
        WriteOptionalAttribute(writer, "implMaxOccurs", item.ImplementationMaximum);
        WriteOptionalAttribute(writer, "lengthId", item.LengthId);
        WritePedigree(writer, item.Pedigree);
        WriteExtensionAttributes(writer, item.Extensions);
        if (options.IncludeBehavior) OrchestraBehaviorEmitter.WriteRules(writer, item.Rules, namespaceUri);
        WriteAnnotations(writer, item.Annotations, namespaceUri);
        WriteEncodings(writer, item.Encodings, namespaceUri);
        WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteScenarios(XmlWriter writer, List<OrchestraScenarioDefinition> source, OrchestraWriteOptions options, string namespaceUri)
    {
        if (source.Count == 0) return;
        List<OrchestraScenarioDefinition> items = new List<OrchestraScenarioDefinition>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => CompareNames(left.Name, right.Name));
        writer.WriteStartElement("fixr", "scenarios", namespaceUri);
        for (int index = 0; index < items.Count; index++)
        {
            OrchestraScenarioDefinition item = items[index];
            writer.WriteStartElement("fixr", "scenario", namespaceUri);
            WriteOptionalAttribute(writer, "name", item.Name);
            WriteOptionalAttribute(writer, "scenarioRef", item.ParentName);
            WritePedigree(writer, item.Pedigree);
            WriteExtensionAttributes(writer, item.Extensions);
            WriteAnnotations(writer, item.Annotations, namespaceUri);
            WriteExtensions(writer, item.Extensions);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteEncodings(XmlWriter writer, List<OrchestraEncoding> encodings, string namespaceUri)
    {
        if (encodings.Count == 0) return;
        writer.WriteStartElement("fixr", "encodings", namespaceUri);
        for (int index = 0; index < encodings.Count; index++) WriteEncoding(writer, encodings[index], namespaceUri);
        writer.WriteEndElement();
    }

    internal static void WriteEncoding(XmlWriter writer, OrchestraEncoding encoding, string namespaceUri)
    {
        writer.WriteStartElement("fixr", "encoding", namespaceUri);
        WriteOptionalAttribute(writer, "standard", encoding.Standard);
        WriteExtensionAttributes(writer, encoding.Extensions);
        WriteAnnotations(writer, encoding.Annotations, namespaceUri);
        WriteExtensions(writer, encoding.Extensions);
        for (int index = 0; index < encoding.SchemaReferences.Count; index++)
        {
            OrchestraExternalSchemaReference reference = encoding.SchemaReferences[index];
            if (reference.Xml.Length != 0) WriteXmlFragment(writer, reference.Xml);
        }
        writer.WriteEndElement();
    }

    internal static void WriteAnnotations(XmlWriter writer, List<OrchestraAnnotation> annotations, string namespaceUri)
    {
        if (annotations.Count == 0) return;
        string orchestraPrefix = NamespacePrefix(namespaceUri);
        writer.WriteStartElement(orchestraPrefix, "annotation", namespaceUri);
        WritePedigree(writer, annotations[0].ContainerPedigree);
        for (int index = 0; index < annotations.Count; index++)
        {
            OrchestraAnnotation item = annotations[index];
            string kind = item.Kind.Length == 0 ? "documentation" : item.Kind;
            writer.WriteStartElement(orchestraPrefix, kind, namespaceUri);
            WriteOptionalAttribute(writer, "purpose", item.Purpose);
            WriteOptionalAttribute(writer, "langId", item.Language);
            WriteOptionalAttribute(writer, "contentType", item.ContentType);
            WriteOptionalAttribute(writer, "specUrl", item.SpecificationUri);
            WritePedigree(writer, item.Pedigree);
            WriteExtensionAttributes(writer, item.Extensions);
            if (item.XmlContent.Length != 0) WriteXmlFragment(writer, item.XmlContent);
            else
            {
                writer.WriteString(item.Content);
                WriteExtensions(writer, item.Extensions);
            }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteExtensions(XmlWriter writer, List<OrchestraExtension> extensions)
    {
        for (int index = 0; index < extensions.Count; index++)
        {
            string xml = extensions[index].Xml.Trim();
            if (xml.StartsWith("<", StringComparison.Ordinal)) WriteXmlFragment(writer, xml);
        }
    }

    internal static void WriteExtensionAttributes(XmlWriter writer, List<OrchestraExtension> extensions)
    {
        for (int index = 0; index < extensions.Count; index++)
        {
            OrchestraExtension extension = extensions[index];
            string xml = extension.Xml.Trim();
            if (xml.Length == 0 || xml.StartsWith("<", StringComparison.Ordinal)) continue;
            int separator = xml.IndexOf('=');
            if (separator < 0) continue;
            string encoded = xml.Substring(separator + 1).Trim();
            if (encoded.Length >= 2) encoded = encoded.Substring(1, encoded.Length - 2);
            XmlDocument document = new XmlDocument();
            XmlAttribute attribute = document.CreateAttribute(extension.Prefix, extension.Name, extension.NamespaceUri);
            attribute.InnerXml = encoded;
            writer.WriteAttributeString(extension.Prefix, extension.Name, extension.NamespaceUri, attribute.Value);
        }
    }

    internal static void WriteXmlFragment(XmlWriter writer, string xml)
    {
        XmlDocument document = new XmlDocument();
        XmlDocumentFragment fragment = document.CreateDocumentFragment();
        fragment.InnerXml = xml;
        fragment.WriteTo(writer);
    }

    internal static void WriteDefinitionAttributes(XmlWriter writer, string id, string name, string abbreviation, string scenario, string category, string rendering)
    {
        WriteOptionalAttribute(writer, "id", id);
        WriteOptionalAttribute(writer, "name", name);
        WriteOptionalAttribute(writer, "abbrName", abbreviation);
        WriteScenarioAttribute(writer, scenario);
        WriteOptionalAttribute(writer, "category", category);
        WriteOptionalAttribute(writer, "rendering", rendering);
    }

    internal static void WritePedigree(XmlWriter writer, OrchestraPedigree pedigree)
    {
        WriteOptionalAttribute(writer, "added", pedigree.AddedVersion);
        WriteOptionalAttribute(writer, "addedEP", pedigree.AddedExtensionPack);
        WriteOptionalAttribute(writer, "changeType", pedigree.ChangeType);
        WriteOptionalAttribute(writer, "deprecated", pedigree.DeprecatedVersion);
        WriteOptionalAttribute(writer, "deprecatedEP", pedigree.DeprecatedExtensionPack);
        WriteOptionalAttribute(writer, "issue", pedigree.Issue);
        WriteOptionalAttribute(writer, "lastModified", pedigree.LastModifiedVersion);
        WriteOptionalAttribute(writer, "replaced", pedigree.ReplacedVersion);
        WriteOptionalAttribute(writer, "replacedEP", pedigree.ReplacedExtensionPack);
        WriteOptionalAttribute(writer, "replacedByField", pedigree.ReplacedByFieldId);
        WriteOptionalAttribute(writer, "supported", pedigree.Supported);
        WriteOptionalAttribute(writer, "updated", pedigree.UpdatedVersion);
        WriteOptionalAttribute(writer, "updatedEP", pedigree.UpdatedExtensionPack);
    }

    internal static void WritePresenceAttribute(XmlWriter writer, string text, FixPresence presence)
    {
        string value = text;
        if (value.Length == 0 && presence == FixPresence.Required) value = "required";
        else if (value.Length == 0 && presence == FixPresence.Forbidden) value = "forbidden";
        else if (value.Length == 0 && presence == FixPresence.Ignored) value = "ignored";
        else if (value.Length == 0 && presence == FixPresence.Constant) value = "constant";
        WriteOptionalAttribute(writer, "presence", value);
    }

    internal static void WriteScenarioAttribute(XmlWriter writer, string scenario)
    {
        if (scenario.Length != 0 && scenario != "base") writer.WriteAttributeString("scenario", scenario);
    }

    internal static void WriteOptionalAttribute(XmlWriter writer, string name, string value)
    {
        if (value != null && value.Length != 0) writer.WriteAttributeString(name, value);
    }

    internal static int CompareNames(string left, string right)
    {
        return string.CompareOrdinal(left, right);
    }

    internal static int CompareDefinitions(string leftName, string leftScenario, string leftId, string rightName, string rightScenario, string rightId)
    {
        int comparison = string.CompareOrdinal(leftName, rightName);
        if (comparison == 0) comparison = string.CompareOrdinal(leftScenario, rightScenario);
        if (comparison == 0) comparison = string.CompareOrdinal(leftId, rightId);
        return comparison;
    }

    internal static string RepositoryNamespace(OrchestraVersion version)
    {
        return version == OrchestraVersion.Version10 ? OrchestraXmlLoader.RepositoryVersion10Namespace : OrchestraXmlLoader.RepositoryVersion11Namespace;
    }

    internal static string InterfacesNamespace(OrchestraVersion version)
    {
        return version == OrchestraVersion.Version10 ? OrchestraXmlLoader.InterfacesVersion10Namespace : OrchestraXmlLoader.InterfacesVersion11Namespace;
    }

    internal static string NamespacePrefix(string namespaceUri)
    {
        if (namespaceUri == OrchestraXmlLoader.InterfacesVersion10Namespace || namespaceUri == OrchestraXmlLoader.InterfacesVersion11Namespace) return "fixi";
        return "fixr";
    }

    internal static void AddDiagnostic(List<OrchestraDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string path)
    {
        OrchestraDiagnostic diagnostic = new OrchestraDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ElementPath = path;
        diagnostics.Add(diagnostic);
    }

    internal static bool HasErrors(List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            ValidationSeverity severity = diagnostics[index].Severity;
            if (severity == ValidationSeverity.Error || severity == ValidationSeverity.Fatal) return true;
        }
        return false;
    }
}
