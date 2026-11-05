using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class RepositoryImporterFixtureTests
{
    [Test]
    public void ImporterNormalizesEverySupportedEntityPropertyAndReferenceShape()
    {
        RepositoryImportResult result = ImportRaw(CreateEveryShapeFixture());
        RepositorySnapshot snapshot = RequireSnapshot(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceDuplicateIdentifierCount, Is.EqualTo(0));
            Assert.That(result.SourceUnresolvedReferenceCount, Is.EqualTo(0));
            Assert.That(result.NormalizedReferenceCount,
                Is.EqualTo(result.SourceReferenceCount));
            Assert.That(snapshot.BusinessComponents, Has.Count.EqualTo(2));
            Assert.That(snapshot.BusinessElements, Has.Count.EqualTo(2));
            Assert.That(snapshot.MessageComponents, Has.Count.EqualTo(2));
            Assert.That(snapshot.MessageElements, Has.Count.EqualTo(3));
            Assert.That(snapshot.Choices, Has.Count.EqualTo(3));
            Assert.That(snapshot.DataTypes, Has.Count.EqualTo(20));
            Assert.That(snapshot.DataTypes.Select(type => type.Kind), Is.EqualTo(new[]
            {
                DataTypeKind.Text,
                DataTypeKind.String,
                DataTypeKind.Identifier,
                DataTypeKind.Boolean,
                DataTypeKind.Indicator,
                DataTypeKind.Decimal,
                DataTypeKind.Amount,
                DataTypeKind.Quantity,
                DataTypeKind.Rate,
                DataTypeKind.Binary,
                DataTypeKind.Date,
                DataTypeKind.DateTime,
                DataTypeKind.Day,
                DataTypeKind.Duration,
                DataTypeKind.Month,
                DataTypeKind.MonthDay,
                DataTypeKind.Time,
                DataTypeKind.Year,
                DataTypeKind.YearMonth,
                DataTypeKind.Schema
            }));
            Assert.That(snapshot.CodeSets, Has.Count.EqualTo(3));
            Assert.That(snapshot.ExternalSchemas, Has.Count.EqualTo(2));
            Assert.That(snapshot.BusinessAreas, Has.Count.EqualTo(1));
            Assert.That(snapshot.BusinessProcesses, Has.Count.EqualTo(1));
            Assert.That(snapshot.BusinessRoles, Has.Count.EqualTo(1));
            Assert.That(snapshot.Participants, Has.Count.EqualTo(1));
            Assert.That(snapshot.BusinessTransactions, Has.Count.EqualTo(1));
            Assert.That(snapshot.MessageTransmissions, Has.Count.EqualTo(1));
            Assert.That(snapshot.MessageChoreographies, Has.Count.EqualTo(1));
            Assert.That(snapshot.Conversations, Has.Count.EqualTo(1));
            Assert.That(snapshot.ConvergenceDefinitions, Has.Count.EqualTo(1));
            Assert.That(snapshot.MessagingEndpoints, Has.Count.EqualTo(1));
            Assert.That(snapshot.Transports, Has.Count.EqualTo(6));
            Assert.That(snapshot.MessageSets, Has.Count.EqualTo(3));
            Assert.That(snapshot.MessageDefinitions, Has.Count.EqualTo(1));
            Assert.That(snapshot.MessageBuildingBlocks, Has.Count.EqualTo(1));
            Assert.That(snapshot.Syntaxes, Has.Count.EqualTo(1));
            Assert.That(snapshot.Encodings, Has.Count.EqualTo(1));
            Assert.That(snapshot.SyntaxMessageSchemes, Has.Count.EqualTo(1));
            Assert.That(snapshot.References.All(reference => reference.IsResolved), Is.True);
        });

        BusinessComponentDefinition account = snapshot.BusinessComponents.Single(
            component => component.Identifier == "_account");
        MessageDefinitionMetadata message = snapshot.MessageDefinitions.Single();
        MessageTransmissionDefinition transmission = snapshot.MessageTransmissions.Single();
        Assert.Multiple(() =>
        {
            Assert.That(account.SuperType?.TargetIdentifier, Is.EqualTo("_base"));
            Assert.That(account.Registration.SemanticMarkup.Single().Elements.Single().Value,
                Is.EqualTo("Ledger Account"));
            Assert.That(account.Registration.Doclets.Single().Value,
                Is.EqualTo("Account documentation."));
            Assert.That(account.Registration.Constraints.Single().Expression,
                Is.EqualTo("balance >= 0"));
            Assert.That(snapshot.CodeSets[0].Codes.Single().Value, Is.EqualTo("ACTV"));
            Assert.That(snapshot.ExternalSchemas[0].NamespaceUris,
                Is.EqualTo(new[] { "urn:test:one", "urn:test:two" }));
            Assert.That(message.MessageIdentifier.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(message.BuildingBlocks.Single().TargetIdentifier,
                Is.EqualTo("_buildingBlock"));
            Assert.That(transmission.Sender.TargetIdentifier, Is.EqualTo("_participant"));
            Assert.That(transmission.Receivers.Single().TargetIdentifier,
                Is.EqualTo("_participant"));
            Assert.That(snapshot.ConvergenceDefinitions.Single().SemanticMarkup.Single()
                .Elements.Single().Value, Is.EqualTo("ISO mapping"));
        });
    }

    [Test]
    public void ImporterResolvesForwardBackwardAndSpaceDelimitedReferencesInSourceOrder()
    {
        RepositoryImportResult first = ImportRaw(CreateReferenceFixture());
        RepositoryImportResult second = ImportRaw(CreateReferenceFixture());
        RepositorySnapshot snapshot = RequireSnapshot(first);
        BusinessComponentDefinition firstComponent = snapshot.BusinessComponents[0];

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(first.SourceUnresolvedReferenceCount, Is.EqualTo(0));
            Assert.That(firstComponent.SuperType?.TargetIdentifier, Is.EqualTo("_later"));
            Assert.That(firstComponent.DerivedMessageComponents.Select(
                    reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_later", "_last" }));
            Assert.That(snapshot.BusinessComponents[1].Registration.NextVersions.Single()
                .TargetIdentifier, Is.EqualTo("_last"));
            Assert.That(snapshot.References.Select(ReferenceSignature),
                Is.EqualTo(RequireSnapshot(second).References.Select(ReferenceSignature)));
        });
    }

    [Test]
    public void ImporterPreservesDuplicatesUnknownContentAndResolutionDiagnostics()
    {
        RepositoryImportResult result = ImportRaw(CreateUnknownAndDuplicateFixture());
        RepositorySnapshot snapshot = RequireSnapshot(result);
        RepositoryUnknownData extension = snapshot.UnknownContent.Single(
            unknown => unknown.Identifier == "_extension");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("repository.import.integrity"));
            Assert.That(result.SourceDuplicateIdentifierCount, Is.EqualTo(1));
            Assert.That(result.SourceUnresolvedReferenceCount, Is.EqualTo(3));
            Assert.That(result.DiagnosticCodes, Does.Contain(
                "repository.integrity.identifier_duplicate"));
            Assert.That(result.DiagnosticCodes, Does.Contain(
                "repository.xmi.reference_missing"));
            Assert.That(result.DiagnosticCodes, Does.Contain(
                "repository.xmi.reference_ambiguous"));
            Assert.That(snapshot.UnknownContent.Select(unknown => unknown.Name.LocalName),
                Is.EqualTo(new[] { "superType", "futureFlag", "extension", "knownSlot" }));
            Assert.That(extension.TypeName, Is.EqualTo("future:FutureClassifier"));
            Assert.That(extension.Value, Is.EqualTo("PrefixSuffix"));
            Assert.That(extension.Children.Select(child => child.Name.LocalName),
                Is.EqualTo(new[] { "flag", "customValue", "superType", "detail" }));
            Assert.That(extension.References.Select(reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_target", "_insideMissing" }));
        });
    }

    [Test]
    public void ImporterRejectsCorruptAndAmbiguousZipArtifactsAtThePublicBoundary()
    {
        byte[] raw = Encoding.UTF8.GetBytes(CreateReferenceFixture());
        byte[] validZip = CreateZip(new[]
        {
            ("first.iso20022", raw),
            ("second.xml", raw)
        });
        byte[] corruptZip = new byte[] { 80, 75, 3, 4, 1, 2, 3, 4 };
        IRepositoryImporter importer = new RepositoryImporter();

        using MemoryStream ambiguousStream = new MemoryStream(validZip);
        RepositoryImportResult ambiguous = importer.ImportZipStream(
            ambiguousStream,
            CreateOptions());
        using MemoryStream corruptStream = new MemoryStream(corruptZip);

        Assert.Multiple(() =>
        {
            Assert.That(ambiguous.Success, Is.False);
            Assert.That(ambiguous.ErrorCode,
                Is.EqualTo("artifact.archive.entry_ambiguous"));
            Assert.Throws<InvalidDataException>(() => importer.ImportZipStream(
                corruptStream,
                CreateOptions()));
        });
    }

    [Test]
    public void ImporterEnforcesArtifactArchiveXmlAndGraphLimits()
    {
        byte[] raw = Encoding.UTF8.GetBytes(CreateReferenceFixture());
        RepositoryImportOptions artifactLimit = CreateOptions();
        artifactLimit.MaxArtifactBytes = raw.Length - 1;
        RepositoryImportOptions elementLimit = CreateOptions();
        elementLimit.Limits.MaxElementCount = 2;
        RepositoryImportOptions relationshipLimit = CreateOptions();
        relationshipLimit.MaxRelationshipCount = 1;
        byte[] zip = CreateZip(new[] { ("fixture.iso20022", raw) });
        RepositoryImportOptions entryLimit = CreateOptions();
        entryLimit.MaxArchiveEntryBytes = raw.Length - 1;
        entryLimit.MaxArchiveExpandedBytes = raw.Length * 2;
        IRepositoryImporter importer = new RepositoryImporter();

        using MemoryStream artifactStream = new MemoryStream(raw);
        RepositoryImportResult artifactResult = importer.ImportRawStream(
            artifactStream,
            artifactLimit);
        using MemoryStream entryStream = new MemoryStream(zip);
        RepositoryImportResult entryResult = importer.ImportZipStream(entryStream, entryLimit);

        Assert.Multiple(() =>
        {
            Assert.That(artifactResult.ErrorCode, Is.EqualTo("artifact.input.limit"));
            Assert.That(entryResult.ErrorCode, Is.EqualTo("artifact.archive.entry_size"));
            Assert.Throws<XmlException>(() => importer.ImportRawStream(
                new MemoryStream(raw),
                elementLimit));
            Assert.Throws<XmlException>(() => importer.ImportRawStream(
                new MemoryStream(raw),
                relationshipLimit));
        });
    }

    [Test]
    public void ImporterObservesCancellationRaisedWhileCopyingTheArtifact()
    {
        byte[] raw = Encoding.UTF8.GetBytes(CreateEveryShapeFixture());
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        using CancelAfterReadStream stream = new CancelAfterReadStream(raw, cancellation);
        RepositoryImportOptions options = CreateOptions();
        options.CancellationToken = cancellation.Token;

        Assert.Throws<OperationCanceledException>(() =>
            new RepositoryImporter().ImportRawStream(stream, options));
        Assert.That(stream.BytesRead, Is.EqualTo(raw.Length));
    }

    [Test]
    public void EveryImportEntryPointReportsIndependentMissingArguments()
    {
        IRepositoryImporter importer = new RepositoryImporter();
        RepositoryImportOptions options = CreateOptions();
        using MemoryStream input = new MemoryStream();

        RepositoryImportResult rawInput = importer.ImportRawStream(null, options);
        RepositoryImportResult rawOptions = importer.ImportRawStream(input, null);
        RepositoryImportResult zipInput = importer.ImportZipStream(null, options);
        RepositoryImportResult zipOptions = importer.ImportZipStream(input, null);
        RepositoryImportResult rawPath = importer.ImportRawFile(null, options);
        RepositoryImportResult rawFileOptions = importer.ImportRawFile("unused", null);
        RepositoryImportResult zipPath = importer.ImportZipFile(null, options);
        RepositoryImportResult zipFileOptions = importer.ImportZipFile("unused", null);

        Assert.Multiple(() =>
        {
            Assert.That(rawInput.ErrorCode, Is.EqualTo("repository.import.input_missing"));
            Assert.That(rawOptions.ErrorCode, Is.EqualTo("repository.import.options_missing"));
            Assert.That(zipInput.ErrorCode, Is.EqualTo("repository.import.input_missing"));
            Assert.That(zipOptions.ErrorCode, Is.EqualTo("repository.import.options_missing"));
            Assert.That(rawPath.ErrorCode, Is.EqualTo("repository.import.input_missing"));
            Assert.That(rawFileOptions.ErrorCode,
                Is.EqualTo("repository.import.options_missing"));
            Assert.That(zipPath.ErrorCode, Is.EqualTo("repository.import.input_missing"));
            Assert.That(zipFileOptions.ErrorCode,
                Is.EqualTo("repository.import.options_missing"));
        });
    }

    [Test]
    public void MissingLimitsAndUnsupportedMetamodelReturnStableFailures()
    {
        RepositoryImportOptions noLimits = CreateOptions();
        noLimits.Limits = null!;
        RepositoryImportResult limits = new RepositoryImporter().ImportRawStream(
            new MemoryStream("<root />"u8.ToArray()), noLimits);
        RepositoryImportResult metamodel = new RepositoryImporter().ImportRawStream(
            new MemoryStream("<root />"u8.ToArray()), CreateOptions());

        Assert.Multiple(() =>
        {
            Assert.That(limits.ErrorCode,
                Is.EqualTo("repository.import.limits_missing"));
            Assert.That(metamodel.Success, Is.False);
            Assert.That(metamodel.ErrorCode, Is.EqualTo("repository.root.unsupported"));
        });
    }

    [Test]
    public void ReferenceDeduplicationAndFailureFactoriesCoverBoundaryState()
    {
        RepositoryReference first = new RepositoryReference
        {
            SourceIdentifier = "source",
            PropertyName = "property",
            TargetIdentifier = "target"
        };
        first.Location.SourceName = null!;
        RepositoryReference duplicate = new RepositoryReference
        {
            SourceIdentifier = "source",
            PropertyName = "property",
            TargetIdentifier = "target"
        };
        duplicate.Location.SourceName = null!;
        RepositorySnapshot snapshot = new RepositorySnapshot();
        snapshot.References.Add(first);
        snapshot.References.Add(duplicate);
        RepositoryImporter.DeduplicateSnapshotReferences(snapshot);

        RepositoryImportOptions artifactLarger = CreateOptions();
        artifactLarger.MaxArtifactBytes = 20;
        artifactLarger.MaxArchiveEntryBytes = 10;
        RepositoryImportOptions entryLarger = CreateOptions();
        entryLarger.MaxArtifactBytes = 10;
        entryLarger.MaxArchiveEntryBytes = 20;

        RepositoryArtifactOpenResult opened = new RepositoryArtifactOpenResult();
        EcoreMetamodelDiscoveryResult discovery = new EcoreMetamodelDiscoveryResult
        {
            ErrorCode = "metamodel.error",
            ErrorMessage = "unsupported"
        };
        RepositoryImportResult discoveryFailure = RepositoryImporter.CreateDiscoveryFailure(
            opened, discovery);
        RepositoryImportResult invalid = RepositoryImporter.CreateImportInputFailure(
            false, false);
        RepositoryImportResult unopened = RepositoryImporter.CreateOpenedFailure(
            opened, "opened.error", "missing");
        RepositoryArtifactOpenResult withContent = new RepositoryArtifactOpenResult
        {
            Content = new MemoryStream()
        };
        EcoreMetamodelDiscoveryResult withAdapter = new EcoreMetamodelDiscoveryResult
        {
            Adapter = new Iso20022Ecore2013Adapter()
        };

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.References, Is.EqualTo(new[] { first }));
            Assert.That(RepositoryImporter.CreateReferenceIdentityKey(first),
                Does.Contain("0:"));
            Assert.That(RepositoryImporter.SelectCharacterLimit(artifactLarger), Is.EqualTo(20));
            Assert.That(RepositoryImporter.SelectCharacterLimit(entryLarger), Is.EqualTo(20));
            Assert.That(discoveryFailure.ErrorCode, Is.EqualTo("metamodel.error"));
            Assert.That(invalid.ErrorCode, Is.EqualTo("repository.import.input_invalid"));
            Assert.That(unopened.ErrorCode, Is.EqualTo("opened.error"));
            Assert.That(RepositoryImporter.RequireOpenedContent(withContent),
                Is.SameAs(withContent.Content));
            Assert.That(() => RepositoryImporter.RequireOpenedContent(opened),
                Throws.InvalidOperationException);
            Assert.That(RepositoryImporter.RequireDiscoveryAdapter(withAdapter),
                Is.SameAs(withAdapter.Adapter));
            Assert.That(() => RepositoryImporter.RequireDiscoveryAdapter(discovery),
                Throws.InvalidOperationException);
        });
        withContent.Content?.Dispose();
    }

    private static RepositoryImportResult ImportRaw(string xml)
    {
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new RepositoryImporter().ImportRawStream(stream, CreateOptions());
    }

    private static RepositorySnapshot RequireSnapshot(RepositoryImportResult result)
    {
        Assert.That(result.Snapshot, Is.Not.Null, result.ErrorMessage);
        return result.Snapshot!;
    }

    private static RepositoryImportOptions CreateOptions()
    {
        return new RepositoryImportOptions
        {
            SourceName = "minimal-fixture.iso20022",
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.Repository,
                Edition = "fixture",
                Identifier = "minimal",
                Version = "1"
            }
        };
    }

    private static string ReferenceSignature(RepositoryReference reference)
    {
        return reference.SourceIdentifier + "|" + reference.PropertyName + "|" +
            reference.Position + "|" + reference.TargetIdentifier + "|" +
            reference.IsResolved;
    }

    private static byte[] CreateZip((string Name, byte[] Content)[] entries)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach ((string name, byte[] content) in entries)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
                using Stream destination = entry.Open();
                destination.Write(content, 0, content.Length);
            }
        }

        return output.ToArray();
    }

    private static string CreateReferenceFixture()
    {
        return """
            <?xml version='1.0'?>
            <iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore'
                xmlns:xmi='http://www.omg.org/XMI'
                xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                xmi:id='_repository'>
              <dataDictionary xmi:id='_dictionary'>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_first'
                    name='First' superType='_later' derivationComponent='_later _last'/>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_later'
                    name='Later' nextVersions='_last'/>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_last'
                    name='Last' previousVersion='_later'/>
              </dataDictionary>
              <businessProcessCatalogue xmi:id='_catalogue'/>
            </iso:Repository>
            """;
    }

    private static string CreateUnknownAndDuplicateFixture()
    {
        return """
            <?xml version='1.0'?>
            <iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore'
                xmlns:future='urn:example:future'
                xmlns:xmi='http://www.omg.org/XMI'
                xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmi:id='_repository'>
              <dataDictionary xmi:id='_dictionary'>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_known'
                    name='KnownComponent' superType='_missing _ambiguous'
                    future:superType='_notAReference' futureFlag='future-value'>
                  <future:extension xsi:type='future:FutureClassifier' xmi:id='_extension'
                      future:flag='yes' customValue='alpha'
                      superType='_target _insideMissing'>Prefix<future:detail
                        future:level='2'>Payload</future:detail>Suffix</future:extension>
                  <future:knownSlot xsi:type='iso:Text' xmi:id='_knownSlot'
                      name='KnownTypeInUnknownFeature' future:mode='extended'/>
                </topLevelDictionaryEntry>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_target'
                    name='Target'/>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_ambiguous'
                    name='FirstAmbiguous'/>
                <topLevelDictionaryEntry xsi:type='iso:Text' xmi:id='_ambiguous'
                    name='SecondAmbiguous'/>
              </dataDictionary>
              <businessProcessCatalogue xmi:id='_catalogue'/>
            </iso:Repository>
            """;
    }

    private static string CreateEveryShapeFixture()
    {
        return """
            <?xml version='1.0'?>
            <iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore'
                xmlns:xmi='http://www.omg.org/XMI'
                xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                xmi:version='2.0' xmi:id='_repository'>
              <dataDictionary xmi:id='_dictionary'>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_account'
                    name='Account' definition='An account.' registrationStatus='Registered'
                    superType='_base' derivationComponent='_component'
                    derivationElement='_messageAttribute' associationDomain='_businessAssociation'>
                  <semanticMarkup xmi:id='_semantic' type='synonym'>
                    <elements xmi:id='_semanticElement' name='name' value='Ledger Account'/>
                  </semanticMarkup>
                  <doclet xmi:id='_doclet' type='definition'
                      content='Account documentation.'/>
                  <example>Account example</example>
                  <constraint xmi:id='_constraint' name='PositiveBalance'
                      expression='balance &gt;= 0' expressionLanguage='OCL'/>
                  <element xsi:type='iso:BusinessAttribute' xmi:id='_businessAttribute'
                      name='Balance' minOccurs='0' maxOccurs='1' simpleType='_text'
                      derivation='_messageAttribute'/>
                  <element xsi:type='iso:BusinessAssociationEnd' xmi:id='_businessAssociation'
                      name='RelatedAccount' aggregation='Shared' type='_base'/>
                </topLevelDictionaryEntry>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_base'
                    name='BaseAccount' nextVersions='_account'/>
                <topLevelDictionaryEntry xsi:type='iso:MessageComponent' xmi:id='_component'
                    name='AccountComponent' trace='_account' isTechnical='true'>
                  <messageElement xsi:type='iso:MessageAttribute' xmi:id='_messageAttribute'
                      name='Balance' minOccurs='1' maxOccurs='1' xmlTag='Bal'
                      simpleType='_text' businessElementTrace='_businessAttribute'/>
                  <messageElement xsi:type='iso:MessageAssociationEnd' xmi:id='_messageAssociation'
                      name='RelatedAccount' minOccurs='0' maxOccurs='1' xmlTag='RltdAcct'
                      type='_component2' businessComponentTrace='_base'/>
                  <xors xmi:id='_componentXor' name='BalanceOrAccount'
                      impactedElements='_messageAttribute _messageAssociation'/>
                </topLevelDictionaryEntry>
                <topLevelDictionaryEntry xsi:type='iso:MessageComponent' xmi:id='_component2'
                    name='RelatedAccountComponent'/>
                <topLevelDictionaryEntry xsi:type='iso:ChoiceComponent' xmi:id='_choice'
                    name='AccountChoice' trace='_account'>
                  <messageElement xsi:type='iso:MessageAttribute' xmi:id='_choiceAttribute'
                      name='ChoiceValue' simpleType='_text'/>
                </topLevelDictionaryEntry>
                <topLevelDictionaryEntry xsi:type='iso:Text' xmi:id='_text' name='TextType'
                    minLength='1' maxLength='35' pattern='[A-Z]+'/>
                <topLevelDictionaryEntry xsi:type='iso:String' xmi:id='_string' name='StringType'/>
                <topLevelDictionaryEntry xsi:type='iso:IdentifierSet' xmi:id='_identifier'
                    name='IdentifierType' identificationScheme='urn:test:id'/>
                <topLevelDictionaryEntry xsi:type='iso:Boolean' xmi:id='_boolean' name='BooleanType'/>
                <topLevelDictionaryEntry xsi:type='iso:Indicator' xmi:id='_indicator'
                    name='IndicatorType' meaningWhenTrue='Yes' meaningWhenFalse='No'/>
                <topLevelDictionaryEntry xsi:type='iso:Decimal' xmi:id='_decimal' name='DecimalType'/>
                <topLevelDictionaryEntry xsi:type='iso:Amount' xmi:id='_amount' name='AmountType'
                    currencyIdentifierSet='_identifier'/>
                <topLevelDictionaryEntry xsi:type='iso:Quantity' xmi:id='_quantity' name='QuantityType'/>
                <topLevelDictionaryEntry xsi:type='iso:Rate' xmi:id='_rate' name='RateType'
                    baseValue='100' baseUnitCode='PCT'/>
                <topLevelDictionaryEntry xsi:type='iso:Binary' xmi:id='_binary' name='BinaryType'/>
                <topLevelDictionaryEntry xsi:type='iso:Date' xmi:id='_date' name='DateType'/>
                <topLevelDictionaryEntry xsi:type='iso:DateTime' xmi:id='_dateTime' name='DateTimeType'/>
                <topLevelDictionaryEntry xsi:type='iso:Day' xmi:id='_day' name='DayType'/>
                <topLevelDictionaryEntry xsi:type='iso:Duration' xmi:id='_duration' name='DurationType'/>
                <topLevelDictionaryEntry xsi:type='iso:Month' xmi:id='_month' name='MonthType'/>
                <topLevelDictionaryEntry xsi:type='iso:MonthDay' xmi:id='_monthDay' name='MonthDayType'/>
                <topLevelDictionaryEntry xsi:type='iso:Time' xmi:id='_time' name='TimeType'/>
                <topLevelDictionaryEntry xsi:type='iso:Year' xmi:id='_year' name='YearType'/>
                <topLevelDictionaryEntry xsi:type='iso:YearMonth' xmi:id='_yearMonth' name='YearMonthType'/>
                <topLevelDictionaryEntry xsi:type='iso:SchemaType' xmi:id='_schema' name='SchemaType'
                    kind='anyURI'/>
                <topLevelDictionaryEntry xsi:type='iso:CodeSet' xmi:id='_codes' name='StatusCode'
                    derivation='_derivedCodes'>
                  <code xmi:id='_activeCode' name='Active' codeName='ACTV'/>
                </topLevelDictionaryEntry>
                <topLevelDictionaryEntry xsi:type='iso:CodeSet' xmi:id='_derivedCodes'
                    name='DerivedStatusCode' trace='_codes'/>
                <topLevelDictionaryEntry xsi:type='iso:CodeSet' xmi:id='_externalCodes'
                    name='ExternalStatusCode' identificationScheme='urn:test:codes'/>
                <topLevelDictionaryEntry xsi:type='iso:ExternalSchema' xmi:id='_external'
                    name='ExternalContent' processContent='lax' trace='_account'>
                  <namespaceList>urn:test:one</namespaceList>
                  <namespaceList>urn:test:two</namespaceList>
                </topLevelDictionaryEntry>
                <topLevelDictionaryEntry xsi:type='iso:UserDefined' xmi:id='_userDefined'
                    name='UserContent' namespace='any' processContents='skip'/>
              </dataDictionary>
              <businessProcessCatalogue xmi:id='_catalogue'>
                <topLevelCatalogueEntry xsi:type='iso:BusinessArea' xmi:id='_area'
                    name='Payments' code='pacs' registrationStatus='Registered'>
                  <messageDefinition xmi:id='_message' name='CreditTransferV10'
                      registrationStatus='Registered' messageSet='_set'
                      choreography='_choreography' trace='_transmission' derivation='_scheme'>
                    <messageBuildingBlock xmi:id='_buildingBlock' name='Account'
                        minOccurs='1' maxOccurs='1' complexType='_component'/>
                    <xors xmi:id='_definitionXor' name='AccountRule'
                        impactedMessageBuildingBlocks='_buildingBlock'/>
                    <messageDefinitionIdentifier businessArea='pacs'
                        messageFunctionality='008' flavour='001' version='10'/>
                  </messageDefinition>
                </topLevelCatalogueEntry>
                <topLevelCatalogueEntry xsi:type='iso:BusinessProcess' xmi:id='_process'
                    name='PaymentProcess' businessProcessTrace='_transaction'>
                  <businessRole xmi:id='_role' name='AgentRole'
                      businessRoleTrace='_participant'/>
                </topLevelCatalogueEntry>
                <topLevelCatalogueEntry xsi:type='iso:BusinessTransaction' xmi:id='_transaction'
                    name='PaymentTransaction' businessProcessTrace='_process'
                    messageTransportMode='_mode' trace='_choreography'>
                  <participant xmi:id='_participant' name='Agent' minOccurs='1' maxOccurs='1'
                      businessRoleTrace='_role' sends='_send' receives='_receive'/>
                  <transmission xmi:id='_transmission' name='PaymentTransmission'
                      derivation='_message'>
                    <send xmi:id='_send' sender='_participant'/>
                    <receive xmi:id='_receive' receiver='_participant'/>
                  </transmission>
                </topLevelCatalogueEntry>
                <topLevelCatalogueEntry xsi:type='iso:MessageChoreography' xmi:id='_choreography'
                    name='PaymentChoreography' businessTransactionTrace='_transaction'
                    messageDefinition='_message'/>
                <topLevelCatalogueEntry xsi:type='iso:Conversation' xmi:id='_conversation'
                    name='PaymentConversation'/>
                <topLevelCatalogueEntry xsi:type='iso:ConvergenceDocumentation'
                    xmi:id='_convergence' name='IndustryConvergence'
                    definition='Maps an industry message set to ISO 20022.'>
                  <semanticMarkup xmi:id='_convergenceMarkup' type='mapping'>
                    <elements xmi:id='_convergenceElement' name='summary'
                        value='ISO mapping'/>
                  </semanticMarkup>
                </topLevelCatalogueEntry>
                <topLevelCatalogueEntry xsi:type='iso:MessageTransportMode' xmi:id='_mode'
                    name='ReliableMode' maximumMessageSize='2048'
                    businessTransaction='_transaction'/>
                <topLevelCatalogueEntry xsi:type='iso:MessageTransportSystem' xmi:id='_system'
                    name='PaymentNetwork'>
                  <endpoint xmi:id='_endpoint' name='AgentEndpoint' location='_address'
                      sentMessage='_transportMessage' receivedMessage='_transportMessage'/>
                </topLevelCatalogueEntry>
                <topLevelCatalogueEntry xsi:type='iso:Address' xmi:id='_address'
                    name='EndpointAddress' endpoint='_endpoint' broadCastList='_broadcast'/>
                <topLevelCatalogueEntry xsi:type='iso:BroadcastList' xmi:id='_broadcast'
                    name='Broadcast' address='_address'/>
                <topLevelCatalogueEntry xsi:type='iso:TransportMessage' xmi:id='_transportMessage'
                    name='TransportMessage' sender='_endpoint' receiver='_endpoint'
                    messageInstance='_instance'/>
                <topLevelCatalogueEntry xsi:type='iso:MessageInstance' xmi:id='_instance'
                    name='MessageInstance' transportMessage='_transportMessage'/>
                <topLevelCatalogueEntry xsi:type='iso:MessageSet' xmi:id='_set'
                    name='PaymentSet' generatedSyntax='_syntax' validEncoding='_encoding'
                    messageDefinition='_message'/>
                <topLevelCatalogueEntry xsi:type='iso:IndustryMessageSet' xmi:id='_industrySet'
                    name='IndustrySet'/>
                <topLevelCatalogueEntry xsi:type='iso:ISO15022MessageSet' xmi:id='_iso15022Set'
                    name='ISO15022Set'/>
                <topLevelCatalogueEntry xsi:type='iso:Syntax' xmi:id='_syntax' name='XML'
                    possibleEncodings='_encoding' generatedFor='_set'/>
                <topLevelCatalogueEntry xsi:type='iso:Encoding' xmi:id='_encoding' name='UTF-8'
                    syntax='_syntax' messageSet='_set'/>
                <topLevelCatalogueEntry xsi:type='iso:SyntaxMessageScheme' xmi:id='_scheme'
                    name='PaymentScheme' messageDefinitionTrace='_message'
                    syntax='_syntax' encoding='_encoding'/>
              </businessProcessCatalogue>
            </iso:Repository>
            """;
    }

    private sealed class CancelAfterReadStream : Stream
    {
        private readonly MemoryStream source;
        private readonly CancellationTokenSource cancellation;

        internal CancelAfterReadStream(byte[] bytes, CancellationTokenSource cancellationSource)
        {
            source = new MemoryStream(bytes, false);
            cancellation = cancellationSource;
        }

        internal int BytesRead;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            BytesRead += read;
            if (read > 0)
            {
                cancellation.Cancel();
            }

            return read;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                source.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
