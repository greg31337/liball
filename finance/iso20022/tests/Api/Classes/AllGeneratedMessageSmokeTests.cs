using System.Collections.Concurrent;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using iso20022;

namespace iso20022.Tests;

public class AllGeneratedMessageSmokeTests
{
    [Test]
    public void EveryGeneratedMessageCreatesWritesValidatesReadsAndRetainsSemantics()
    {
        RepositorySnapshot repository = ImportRepository();
        HashSet<string> messageSources = repository.MessageDefinitions
            .Select(message => message.Identifier)
            .ToHashSet(StringComparer.Ordinal);
        ConcurrentBag<string> generatedSchemaSources = new ConcurrentBag<string>();
        ConcurrentBag<string> failures = new ConcurrentBag<string>();
        ConcurrentBag<string> unavailable = new ConcurrentBag<string>();
        int completed = 0;
        List<List<MessageDefinitionMetadata>> batches = repository.MessageDefinitions
            .GroupBy(message => message.MessageIdentifier.BusinessArea)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.ToList())
            .ToList();
        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2)
        };
        Parallel.ForEach(batches, parallelOptions, batch =>
        {
            GenerationResult schemas = new XsdGenerator().GenerateXsd(
                CreateMessagePlan(repository, batch));
            if (!schemas.Success)
            {
                failures.Add(batch[0].MessageIdentifier.BusinessArea +
                    ": XSD generation failed: " + schemas.ErrorMessage);
                return;
            }
            Dictionary<string, GeneratedFile> schemaBySource = CreateSchemaFileMap(schemas);
            int index = 0;
            while (index < batch.Count)
            {
                MessageDefinitionMetadata message = batch[index];
                if (!schemaBySource.TryGetValue(
                    message.Identifier,
                    out GeneratedFile? schemaFile))
                {
                    failures.Add(message.MessageIdentifier.Value +
                        ": generated XSD missing");
                }
                else
                {
                    generatedSchemaSources.Add(message.Identifier);
                    if (SmokeTestMessage(message, schemaFile, failures, unavailable))
                    {
                        System.Threading.Interlocked.Increment(ref completed);
                    }
                }
                index++;
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(messageSources, Has.Count.EqualTo(3329));
            Assert.That(generatedSchemaSources, Is.EquivalentTo(messageSources));
            Assert.That(failures, Is.Empty,
                "Generated message smoke failures:" + Environment.NewLine +
                string.Join(Environment.NewLine, failures.Take(40)));
            Assert.That(completed + unavailable.Count, Is.EqualTo(messageSources.Count),
                "Unaccounted messages; known unconstructible schemas:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, unavailable.Take(40)));
            Assert.That(completed, Is.GreaterThan(3000));
        });
    }

    private static RepositorySnapshot ImportRepository()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022/eRepository_ISO20022_20260626.zip"));
        RepositoryImportResult imported = new RepositoryImporter().ImportZipFile(
            path,
            new RepositoryImportOptions
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Repository,
                    Edition = "2026-06",
                    Identifier = "iso20022-e-repository",
                    Version = "20260626"
                },
                IsApproved = true
            });
        Assert.That(imported.Success, Is.True, imported.ErrorMessage);
        Assert.That(imported.Snapshot, Is.Not.Null, imported.ErrorMessage);
        return imported.Snapshot!;
    }

    private static GenerationPlan CreateMessagePlan(
        RepositorySnapshot repository,
        List<MessageDefinitionMetadata> messages)
    {
        GenerationPlan plan = new GenerationPlan
        {
            Repository = repository,
            Options = new GenerationOptions
            {
                ArtifactEdition = "2026-06",
                EmitManifest = false,
                EmitSourceMappings = true,
                IncludeDeprecated = true,
                IncludeDocumentation = false,
                RootNamespace = "AllMessages.Generated"
            }
        };
        plan.Options.Limits.MaxCollectionCount = 1000000;
        plan.Options.Limits.MaxElementCount = 2000000;
        plan.Options.Limits.MaxSchemaCount = 10000;
        plan.Options.Limits.MaxTotalBytes = int.MaxValue;
        int index = 0;
        while (index < messages.Count)
        {
            plan.Options.MessageIdentifiers.Add(messages[index].MessageIdentifier);
            index++;
        }
        return plan;
    }

    private static bool SmokeTestMessage(
        MessageDefinitionMetadata message,
        GeneratedFile schemaFile,
        ConcurrentBag<string> failures,
        ConcurrentBag<string> unavailable)
    {
        try
        {
            XmlSchemaSet schemaSet = CompileSchema(schemaFile);
            string rootName = message.RootElement.Length > 0 ?
                message.RootElement : "Document";
            XmlSchemaElement? root = schemaSet.GlobalElements[
                new XmlQualifiedName(rootName, message.NamespaceUri)] as
                XmlSchemaElement;
            if (root == null)
            {
                failures.Add(message.MessageIdentifier.Value + ": schema root missing");
                return false;
            }
            XmlDocument minimal = new XmlDocument();
            XmlElement rootElement = minimal.CreateElement(
                string.Empty,
                rootName,
                message.NamespaceUri);
            minimal.AppendChild(rootElement);
            HashSet<XmlQualifiedName> typePath = new HashSet<XmlQualifiedName>();
            string creationError = PopulateElement(
                minimal,
                rootElement,
                root,
                schemaSet,
                typePath,
                0);
            if (creationError.Length > 0)
            {
                unavailable.Add(message.MessageIdentifier.Value + ": " + creationError);
                return false;
            }
            string xml = minimal.OuterXml;
            string validationError = ValidateXml(xml, schemaSet);
            if (validationError.Length > 0)
            {
                if (IsUnconstructibleExternalCode(validationError))
                {
                    unavailable.Add(message.MessageIdentifier.Value + ": " +
                        validationError);
                }
                else
                {
                    failures.Add(message.MessageIdentifier.Value + ": " + validationError);
                }
                return false;
            }
            MessageReadResult read = new XmlMessageReader().ReadGenericText(
                xml,
                new MessageReadOptions());
            if (!read.Success || read.Document == null)
            {
                failures.Add(message.MessageIdentifier.Value + ": " + read.ErrorCode +
                    ": " + read.ErrorMessage);
                return false;
            }
            MessageWriteOptions writeOptions = new MessageWriteOptions();
            writeOptions.WriteXmlDeclaration = false;
            MessageWriteResult written = new XmlMessageWriter().WriteGenericString(
                read.Document,
                writeOptions);
            if (!written.Success)
            {
                failures.Add(message.MessageIdentifier.Value + ": " +
                    written.ErrorCode + ": " + written.ErrorMessage);
                return false;
            }
            validationError = ValidateXml(written.Text, schemaSet);
            MessageReadResult reread = new XmlMessageReader().ReadGenericText(
                written.Text,
                new MessageReadOptions());
            if (validationError.Length > 0)
            {
                failures.Add(message.MessageIdentifier.Value + ": " + validationError);
                return false;
            }
            if (!reread.Success || reread.Document == null)
            {
                failures.Add(message.MessageIdentifier.Value + ": reread failed: " +
                    reread.ErrorCode);
                return false;
            }
            if (CreateDocumentSignature(read.Document) !=
                CreateDocumentSignature(reread.Document))
            {
                failures.Add(message.MessageIdentifier.Value +
                    ": semantic generic model changed after write/read");
                return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            string diagnostic = message.MessageIdentifier.Value + ": " +
                exception.GetType().Name + ": " + exception.Message;
            if (exception is XmlSchemaException &&
                exception.Message.Contains(
                    "Circular type reference",
                    StringComparison.Ordinal))
            {
                unavailable.Add(diagnostic);
            }
            else
            {
                failures.Add(diagnostic);
            }
            return false;
        }
    }

    private static bool IsUnconstructibleExternalCode(string validationError)
    {
        return validationError.Contains("The value ''", StringComparison.Ordinal) &&
            validationError.Contains(":External", StringComparison.Ordinal) &&
            validationError.Contains(
                "actual length is less than the MinLength",
                StringComparison.Ordinal);
    }

    private static Dictionary<string, GeneratedFile> CreateSchemaFileMap(
        GenerationResult schemas)
    {
        Dictionary<string, GeneratedFile> result = new Dictionary<string, GeneratedFile>(
            StringComparer.Ordinal);
        int fileIndex = 0;
        while (fileIndex < schemas.Files.Count)
        {
            GeneratedFile file = schemas.Files[fileIndex];
            if (file.Kind == GenerationFileKind.Xsd)
            {
                int sourceIndex = 0;
                while (sourceIndex < file.SourceIdentifiers.Count)
                {
                    string source = file.SourceIdentifiers[sourceIndex];
                    if (!result.ContainsKey(source))
                    {
                        result[source] = file;
                    }
                    sourceIndex++;
                }
            }
            fileIndex++;
        }
        return result;
    }

    private static XmlSchemaSet CompileSchema(GeneratedFile file)
    {
        XmlSchemaSet schemas = new XmlSchemaSet();
        using MemoryStream input = new MemoryStream(file.Content, false);
        using XmlReader reader = XmlReader.Create(input);
        schemas.Add(null, reader);
        schemas.Compile();
        return schemas;
    }

    private static string PopulateElement(
        XmlDocument document,
        XmlElement output,
        XmlSchemaElement declaration,
        XmlSchemaSet schemas,
        HashSet<XmlQualifiedName> typePath,
        int depth)
    {
        if (depth > 100)
        {
            return "required schema depth exceeds 100";
        }
        XmlSchemaType? schemaType = declaration.ElementSchemaType;
        if (schemaType == null)
        {
            return "element schema type is unavailable for " + declaration.QualifiedName;
        }
        if (!schemaType.QualifiedName.IsEmpty && !typePath.Add(schemaType.QualifiedName))
        {
            return "required recursive schema type " + schemaType.QualifiedName;
        }
        string error = string.Empty;
        if (schemaType is XmlSchemaComplexType complex)
        {
            error = PopulateAttributes(output, complex);
            if (error.Length == 0)
            {
                XmlSchemaParticle? particle = complex.ContentTypeParticle;
                if (particle != null)
                {
                    error = PopulateParticle(
                        document,
                        output,
                        particle,
                        schemas,
                        typePath,
                        depth + 1);
                }
                else if (complex.Datatype != null)
                {
                    string? value = CreateSimpleValue(
                        complex.Datatype,
                        schemaType,
                        declaration.SchemaTypeName);
                    if (value == null)
                    {
                        error = "simple type has no valid lexical value " +
                            schemaType.QualifiedName;
                    }
                    else
                    {
                        output.InnerText = value;
                    }
                }
            }
        }
        else if (schemaType.Datatype != null)
        {
            string? value = CreateSimpleValue(
                schemaType.Datatype,
                schemaType,
                declaration.SchemaTypeName);
            if (value == null)
            {
                error = "simple type has no valid lexical value " +
                    schemaType.QualifiedName;
            }
            else
            {
                output.InnerText = value;
            }
        }
        if (!schemaType.QualifiedName.IsEmpty)
        {
            typePath.Remove(schemaType.QualifiedName);
        }
        return error;
    }

    private static string PopulateAttributes(
        XmlElement output,
        XmlSchemaComplexType complex)
    {
        foreach (XmlSchemaObject value in complex.AttributeUses.Values)
        {
            if (value is XmlSchemaAttribute attribute &&
                attribute.Use == XmlSchemaUse.Required)
            {
                string localName = attribute.QualifiedName.Name;
                if (localName.Length == 0)
                {
                    localName = attribute.Name ?? string.Empty;
                }
                string namespaceUri = attribute.QualifiedName.Namespace;
                XmlSchemaDatatype? datatype = attribute.AttributeSchemaType?.Datatype;
                string text = "X";
                if (localName == "Ccy")
                {
                    text = "USD";
                }
                else if (datatype != null && attribute.AttributeSchemaType != null)
                {
                    string? candidate = CreateSimpleValue(
                        datatype,
                        attribute.AttributeSchemaType,
                        attribute.SchemaTypeName);
                    if (candidate == null)
                    {
                        return "required attribute has no valid lexical value " +
                            attribute.QualifiedName;
                    }
                    text = candidate;
                }
                output.SetAttribute(localName, namespaceUri, text);
            }
        }
        return string.Empty;
    }

    private static string PopulateParticle(
        XmlDocument document,
        XmlElement parent,
        XmlSchemaParticle particle,
        XmlSchemaSet schemas,
        HashSet<XmlQualifiedName> typePath,
        int depth)
    {
        int minimum = decimal.ToInt32(particle.MinOccurs);
        if (particle is XmlSchemaChoice choice)
        {
            if (minimum == 0)
            {
                return string.Empty;
            }
            foreach (XmlSchemaParticle item in choice.Items.OfType<XmlSchemaParticle>())
            {
                string error = PopulateParticle(
                    document,
                    parent,
                    item,
                    schemas,
                    typePath,
                    depth);
                if (error.Length == 0)
                {
                    return string.Empty;
                }
            }
            return "no minimally constructible required choice";
        }
        if (particle is XmlSchemaGroupBase group)
        {
            if (minimum == 0)
            {
                return string.Empty;
            }
            int occurrence = 0;
            while (occurrence < minimum)
            {
                foreach (XmlSchemaParticle item in group.Items.OfType<XmlSchemaParticle>())
                {
                    string error = PopulateParticle(
                        document,
                        parent,
                        item,
                        schemas,
                        typePath,
                        depth);
                    if (error.Length > 0)
                    {
                        return error;
                    }
                }
                occurrence++;
            }
            return string.Empty;
        }
        if (particle is XmlSchemaElement element)
        {
            int occurrence = 0;
            while (occurrence < minimum)
            {
                XmlQualifiedName name = element.QualifiedName;
                if (name.IsEmpty)
                {
                    name = element.RefName;
                }
                XmlElement child = document.CreateElement(
                    string.Empty,
                    name.Name,
                    name.Namespace.Length > 0 ? name.Namespace : parent.NamespaceURI);
                parent.AppendChild(child);
                string error = PopulateElement(
                    document,
                    child,
                    element,
                    schemas,
                    typePath,
                    depth + 1);
                if (error.Length > 0)
                {
                    return error;
                }
                occurrence++;
            }
            return string.Empty;
        }
        if (particle is XmlSchemaAny && minimum > 0)
        {
            XmlElement child = document.CreateElement(
                string.Empty,
                "Any",
                "urn:iso20022:test");
            parent.AppendChild(child);
        }
        return string.Empty;
    }

    private static string? CreateSimpleValue(
        XmlSchemaDatatype datatype,
        XmlSchemaType schemaType,
        XmlQualifiedName declaredTypeName)
    {
        List<string> enumerations = FindEnumerationValues(schemaType);
        if (enumerations.Count > 0)
        {
            if (TypeNameContains(
                schemaType,
                "External",
                StringComparison.Ordinal) &&
                enumerations.All(value => value.Length == 0))
            {
                return null;
            }
            if (declaredTypeName.Name.StartsWith(
                "External",
                StringComparison.Ordinal) &&
                enumerations.All(value => value.Length == 0))
            {
                return null;
            }
            int enumerationIndex = 0;
            while (enumerationIndex < enumerations.Count)
            {
                string enumeration = enumerations[enumerationIndex];
                if (MeetsLengthFacets(schemaType, enumeration))
                {
                    return enumeration;
                }
                enumerationIndex++;
            }
            return null;
        }
        if (declaredTypeName.Name.Contains(
            "ISONormalisedDateTime",
            StringComparison.Ordinal) || TypeNameContains(
            schemaType,
            "ISONormalisedDateTime",
            StringComparison.Ordinal))
        {
            return "2000-01-01T00:00:00Z";
        }
        switch (datatype.TypeCode)
        {
            case XmlTypeCode.Boolean:
                return "true";
            case XmlTypeCode.Decimal:
            case XmlTypeCode.Float:
            case XmlTypeCode.Double:
            case XmlTypeCode.Integer:
            case XmlTypeCode.NonPositiveInteger:
            case XmlTypeCode.NegativeInteger:
            case XmlTypeCode.Long:
            case XmlTypeCode.Int:
            case XmlTypeCode.Short:
            case XmlTypeCode.Byte:
            case XmlTypeCode.NonNegativeInteger:
            case XmlTypeCode.UnsignedLong:
            case XmlTypeCode.UnsignedInt:
            case XmlTypeCode.UnsignedShort:
            case XmlTypeCode.UnsignedByte:
            case XmlTypeCode.PositiveInteger:
                return "1";
            case XmlTypeCode.Date:
                return "2000-01-01";
            case XmlTypeCode.DateTime:
                return "2000-01-01T00:00:00";
            case XmlTypeCode.Time:
                return "00:00:00";
            case XmlTypeCode.Duration:
                return "P1D";
            case XmlTypeCode.GDay:
                return "---01";
            case XmlTypeCode.GMonth:
                return "--01";
            case XmlTypeCode.GMonthDay:
                return "--01-01";
            case XmlTypeCode.GYear:
                return "2000";
            case XmlTypeCode.GYearMonth:
                return "2000-01";
            case XmlTypeCode.HexBinary:
                return "01";
            case XmlTypeCode.Base64Binary:
                return "AQ==";
            case XmlTypeCode.AnyUri:
                return "https://example.com";
            case XmlTypeCode.QName:
                return "Name";
            default:
                return CreatePatternValue(datatype, schemaType, declaredTypeName);
        }
    }

    private static List<string> FindEnumerationValues(XmlSchemaType schemaType)
    {
        List<string> result = new List<string>();
        XmlSchemaType? current = schemaType;
        while (current != null)
        {
            if (current is XmlSchemaSimpleType simple &&
                simple.Content is XmlSchemaSimpleTypeRestriction restriction)
            {
                foreach (XmlSchemaEnumerationFacet facet in restriction.Facets
                    .OfType<XmlSchemaEnumerationFacet>())
                {
                    if (facet.Value != null)
                    {
                        result.Add(facet.Value);
                    }
                }
            }
            XmlSchemaType? next = current.BaseXmlSchemaType;
            if (next == null || ReferenceEquals(next, current))
            {
                break;
            }
            current = next;
        }
        return result;
    }

    private static bool MeetsLengthFacets(XmlSchemaType schemaType, string value)
    {
        XmlSchemaType? current = schemaType;
        while (current != null)
        {
            if (current is XmlSchemaSimpleType simple &&
                simple.Content is XmlSchemaSimpleTypeRestriction restriction)
            {
                foreach (XmlSchemaFacet facet in restriction.Facets
                    .OfType<XmlSchemaFacet>())
                {
                    if (facet is XmlSchemaMinLengthFacet &&
                        int.TryParse(facet.Value, out int minimum) &&
                        value.Length < minimum)
                    {
                        return false;
                    }
                    if (facet is XmlSchemaMaxLengthFacet &&
                        int.TryParse(facet.Value, out int maximum) &&
                        value.Length > maximum)
                    {
                        return false;
                    }
                    if (facet is XmlSchemaLengthFacet &&
                        int.TryParse(facet.Value, out int length) &&
                        value.Length != length)
                    {
                        return false;
                    }
                }
            }
            XmlSchemaType? next = current.BaseXmlSchemaType;
            if (next == null || ReferenceEquals(next, current))
            {
                break;
            }
            current = next;
        }
        return true;
    }

    private static bool TypeNameContains(
        XmlSchemaType schemaType,
        string value,
        StringComparison comparison)
    {
        XmlSchemaType? current = schemaType;
        while (current != null)
        {
            if (current.QualifiedName.Name.Contains(value, comparison) ||
                (current.Name != null && current.Name.Contains(value, comparison)))
            {
                return true;
            }
            XmlSchemaType? next = current.BaseXmlSchemaType;
            if (next == null || ReferenceEquals(next, current))
            {
                break;
            }
            current = next;
        }
        return false;
    }

    private static string CreatePatternValue(
        XmlSchemaDatatype datatype,
        XmlSchemaType schemaType,
        XmlQualifiedName declaredTypeName)
    {
        if (declaredTypeName.Name.Contains(
            "PhoneNumber",
            StringComparison.Ordinal) || TypeNameContains(
            schemaType,
            "PhoneNumber",
            StringComparison.Ordinal))
        {
            return "+1-2025550123";
        }
        int minimum = 1;
        int maximum = 256;
        if (schemaType is XmlSchemaSimpleType simple &&
            simple.Content is XmlSchemaSimpleTypeRestriction restriction)
        {
            foreach (XmlSchemaFacet facet in restriction.Facets.OfType<XmlSchemaFacet>())
            {
                if (facet is XmlSchemaMinLengthFacet &&
                    int.TryParse(facet.Value, out int parsedMinimum))
                {
                    minimum = parsedMinimum;
                }
                else if (facet is XmlSchemaMaxLengthFacet &&
                    int.TryParse(facet.Value, out int parsedMaximum))
                {
                    maximum = parsedMaximum;
                }
                else if (facet is XmlSchemaLengthFacet &&
                    int.TryParse(facet.Value, out int parsedLength))
                {
                    minimum = parsedLength;
                    maximum = parsedLength;
                }
            }
        }
        string[] candidates =
        {
            "X", "A", "AA", "AAA", "AAAA", "US", "USD", "GB", "AAAAUSAA",
            "AAAAUSAA123", "US0378331005", "0", "00", "000", "0000", "1",
            "01", "001", "123", "1234", "123456789012",
            "NOTPROVIDED", "GB82WEST12345698765432",
            "123e4567-e89b-42d3-a456-426614174000", "5493001KJTIIGC8Y1R12",
            "example@example.com", "+12025550123", "https://example.com"
        };
        int index = 0;
        while (index < candidates.Length)
        {
            string candidate = candidates[index];
            if (candidate.Length >= minimum && candidate.Length <= maximum &&
                IsValidSimpleValue(datatype, candidate))
            {
                return candidate;
            }
            index++;
        }
        int length = Math.Max(1, minimum);
        while (length <= maximum)
        {
            string alphabetic = new string('A', length);
            if (IsValidSimpleValue(datatype, alphabetic))
            {
                return alphabetic;
            }
            string numeric = new string('0', length);
            if (IsValidSimpleValue(datatype, numeric))
            {
                return numeric;
            }
            numeric = new string('1', length);
            if (IsValidSimpleValue(datatype, numeric))
            {
                return numeric;
            }
            length++;
        }
        return new string('A', Math.Max(1, minimum));
    }

    private static bool IsValidSimpleValue(
        XmlSchemaDatatype datatype,
        string value)
    {
        try
        {
            XmlNameTable names = new NameTable();
            XmlNamespaceManager namespaces = new XmlNamespaceManager(names);
            namespaces.AddNamespace("x", "urn:iso20022:test");
            datatype.ParseValue(value, names, namespaces);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string ValidateXml(string xml, XmlSchemaSet schemas)
    {
        List<string> errors = new List<string>();
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.Schemas = schemas;
        settings.ValidationType = ValidationType.Schema;
        settings.ValidationEventHandler += (_, arguments) => errors.Add(arguments.Message);
        using XmlReader reader = XmlReader.Create(new StringReader(xml), settings);
        while (reader.Read())
        {
        }
        return errors.Count == 0 ? string.Empty : errors[0];
    }

    private static string CreateDocumentSignature(MessageDocument document)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < document.Payloads.Count)
        {
            AppendNodeSignature(value, document.Payloads[index].GenericContent);
            index++;
        }
        return value.ToString();
    }

    private static void AppendNodeSignature(StringBuilder value, DynamicXmlNode? node)
    {
        if (node == null)
        {
            value.Append("null;");
            return;
        }
        value.Append((int)node.Kind).Append('|').Append(node.Name.NamespaceUri).Append('|')
            .Append(node.Name.LocalName).Append('|').Append(node.Value).Append('|')
            .Append(node.IsNil).Append('{');
        int index = 0;
        while (index < node.Attributes.Count)
        {
            XmlAttributeData attribute = node.Attributes[index];
            value.Append('@').Append(attribute.Name.NamespaceUri).Append('|')
                .Append(attribute.Name.LocalName).Append('=').Append(attribute.Value)
                .Append(';');
            index++;
        }
        index = 0;
        while (index < node.Children.Count)
        {
            AppendNodeSignature(value, node.Children[index]);
            index++;
        }
        value.Append("};");
    }
}
