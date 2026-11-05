using iso20022;

namespace iso20022.Tests;

public class RepositoryFoundationDataTests
{
    [Test]
    public void SnapshotRetainsExactArtifactAndMetamodelProvenance()
    {
        ArtifactManifest manifest = new ArtifactManifest
        {
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.Repository,
                Edition = "2026-06-26",
                Identifier = "eRepository",
                Version = "2013-ecore"
            },
            OriginUri = "https://example.test/eRepository.zip",
            HashAlgorithm = "SHA-256",
            Hash = new IsoBinaryValue { Value = new byte[] { 0x20, 0x02, 0x20, 0x26 } }
        };
        RepositoryProvenance provenance = new RepositoryProvenance
        {
            Artifact = manifest,
            SourceEntryName = "eRepository_ISO20022_20260626.iso20022",
            RepositoryNamespace = "urn:iso:std:iso:20022:2013:ecore",
            MetamodelNamespace = "urn:iso:std:iso:20022:2013:ecore",
            MetamodelVersion = "2013",
            AdapterIdentifier = "iso20022.ecore.2013",
            ImportedAt = new IsoDateTimeValue
            {
                LexicalValue = "2026-08-31T12:00:00Z"
            }
        };
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "_repository",
            Edition = "2026-06-26",
            Provenance = provenance,
            DataDictionary = new RepositoryReference
            {
                SourceIdentifier = "_repository",
                SourceKind = RepositoryEntityKind.Repository,
                PropertyName = "dataDictionary",
                TargetIdentifier = "_dictionary",
                TargetKind = RepositoryEntityKind.DataDictionary,
                IsContainment = true,
                IsResolved = true
            },
            BusinessProcessCatalogue = new RepositoryReference
            {
                SourceIdentifier = "_repository",
                SourceKind = RepositoryEntityKind.Repository,
                PropertyName = "businessProcessCatalogue",
                TargetIdentifier = "_catalogue",
                TargetKind = RepositoryEntityKind.BusinessProcessCatalogue,
                IsContainment = true,
                IsResolved = true
            }
        };
        snapshot.References.Add(snapshot.DataDictionary);
        snapshot.References.Add(snapshot.BusinessProcessCatalogue);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Identifier, Is.EqualTo("_repository"));
            Assert.That(snapshot.Provenance.Artifact, Is.SameAs(manifest));
            Assert.That(snapshot.Provenance.Artifact.Coordinate.Edition, Is.EqualTo("2026-06-26"));
            Assert.That(snapshot.Provenance.SourceEntryName, Is.EqualTo("eRepository_ISO20022_20260626.iso20022"));
            Assert.That(snapshot.Provenance.MetamodelVersion, Is.EqualTo("2013"));
            Assert.That(snapshot.Provenance.ImportedAt?.LexicalValue, Is.EqualTo("2026-08-31T12:00:00Z"));
            Assert.That(snapshot.References.Select(reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_dictionary", "_catalogue" }));
            Assert.That(snapshot.DataDictionary.IsContainment, Is.True);
            Assert.That(snapshot.BusinessProcessCatalogue.TargetKind,
                Is.EqualTo(RepositoryEntityKind.BusinessProcessCatalogue));
        });
    }

    [Test]
    public void RegistrationRetainsLifecycleDocumentationSemanticsAndConstraints()
    {
        SemanticMarkupData synonym = new SemanticMarkupData
        {
            Identifier = "_synonym",
            Type = "synonym"
        };
        synonym.Elements.Add(new SemanticMarkupData { Name = "name", Value = "Settlement Date" });
        synonym.Elements.Add(new SemanticMarkupData { Name = "context", Value = "Securities" });

        RepositoryReference owner = new RepositoryReference
        {
            TargetIdentifier = "_concept",
            TargetKind = RepositoryEntityKind.BusinessComponent,
            IsResolved = true
        };
        ConstraintDefinition constraint = new ConstraintDefinition
        {
            Identifier = "_constraint",
            Name = "RemovalDateRegistrationStatus",
            Definition = "A removed concept must be obsolete.",
            Expression = "removalDate implies registrationStatus = OBSOLETE",
            ExpressionLanguage = "OCL",
            Owner = owner,
            Registration = new RegistrationMetadata
            {
                Status = RegistrationStatus.Registered,
                StatusValue = "Registered"
            }
        };
        RegistrationMetadata registration = new RegistrationMetadata
        {
            Status = RegistrationStatus.Obsolete,
            StatusValue = "Obsolete",
            RegistrationDate = new IsoDateValue { Year = 2013, Month = 4, Day = 1 },
            RemovalDate = new IsoDateValue { Year = 2026, Month = 6, Day = 26 },
            IsDeprecated = true,
            DeprecationReason = "Replaced by the current concept.",
            PreviousVersion = new RepositoryReference { TargetIdentifier = "_previous", IsResolved = true },
            ReplacedBy = new RepositoryReference { TargetIdentifier = "_replacement", IsResolved = true }
        };
        registration.NextVersions.Add(new RepositoryReference { TargetIdentifier = "_next", Position = 0 });
        registration.Examples.Add("Example value");
        registration.Doclets.Add(new SemanticMarkupData { Type = "usage", Value = "Use in settlement flows." });
        registration.SemanticMarkup.Add(synonym);
        registration.Constraints.Add(constraint);

        Assert.Multiple(() =>
        {
            Assert.That(registration.Status, Is.EqualTo(RegistrationStatus.Obsolete));
            Assert.That(registration.StatusValue, Is.EqualTo("Obsolete"));
            Assert.That(registration.RegistrationDate?.Year, Is.EqualTo(2013));
            Assert.That(registration.RemovalDate?.Day, Is.EqualTo(26));
            Assert.That(registration.ReplacedBy?.TargetIdentifier, Is.EqualTo("_replacement"));
            Assert.That(registration.PreviousVersion?.TargetIdentifier, Is.EqualTo("_previous"));
            Assert.That(registration.NextVersions.Single().TargetIdentifier, Is.EqualTo("_next"));
            Assert.That(registration.Doclets.Single().Value, Is.EqualTo("Use in settlement flows."));
            Assert.That(registration.SemanticMarkup.Single().Elements.Select(element => element.Value),
                Is.EqualTo(new[] { "Settlement Date", "Securities" }));
            Assert.That(registration.Constraints.Single().ExpressionLanguage, Is.EqualTo("OCL"));
            Assert.That(registration.Constraints.Single().Owner, Is.SameAs(owner));
        });
    }

    [Test]
    public void ReferenceRetainsOrderingLexicalValueAndResolutionFailure()
    {
        RepositoryReference reference = new RepositoryReference
        {
            SourceIdentifier = "_source",
            SourceKind = RepositoryEntityKind.MessageDefinition,
            PropertyName = "nextVersions",
            Position = 2,
            TargetIdentifier = "_missing",
            TargetKind = RepositoryEntityKind.MessageDefinition,
            RawValue = "_first _second _missing",
            IsContainment = false,
            IsResolved = false,
            ErrorCode = "repository.reference.unresolved",
            ErrorMessage = "The target identifier was not found.",
            Location = new SourceLocation
            {
                SourceName = "repository.iso20022",
                HasLineInformation = true,
                LineNumber = 42,
                ColumnNumber = 17
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(reference.SourceKind, Is.EqualTo(RepositoryEntityKind.MessageDefinition));
            Assert.That(reference.Position, Is.EqualTo(2));
            Assert.That(reference.RawValue, Is.EqualTo("_first _second _missing"));
            Assert.That(reference.IsContainment, Is.False);
            Assert.That(reference.IsResolved, Is.False);
            Assert.That(reference.ErrorCode, Is.EqualTo("repository.reference.unresolved"));
            Assert.That(reference.Location.LineNumber, Is.EqualTo(42));
        });
    }

    [Test]
    public void UnknownContentRetainsAttributesStructuredXmlChildrenAndReferences()
    {
        RepositoryUnknownData extension = new RepositoryUnknownData
        {
            OwnerIdentifier = "_owner",
            OwnerKind = RepositoryEntityKind.DataType,
            Identifier = "_extension",
            TypeName = "future:DataTypeExtension",
            Name = new QualifiedNameData
            {
                NamespaceUri = "urn:example:future",
                LocalName = "extension",
                Prefix = "future"
            },
            RawContent = new RawXmlFragment
            {
                Xml = "<future:extension xmlns:future='urn:example:future' future:mode='new'/>",
                RootName = new QualifiedNameData
                {
                    NamespaceUri = "urn:example:future",
                    LocalName = "extension",
                    Prefix = "future"
                }
            },
            Location = new SourceLocation { HasCharacterOffset = true, CharacterOffset = 4096 }
        };
        extension.Children.Add(new RepositoryUnknownData
        {
            OwnerIdentifier = "_extension",
            OwnerKind = RepositoryEntityKind.Unknown,
            Name = new QualifiedNameData { NamespaceUri = "urn:example:future", LocalName = "mode", Prefix = "future" },
            IsAttribute = true,
            Value = "new"
        });
        extension.References.Add(new RepositoryReference
        {
            SourceIdentifier = "_extension",
            PropertyName = "futureType",
            TargetIdentifier = "_futureType",
            IsResolved = false
        });

        Assert.Multiple(() =>
        {
            Assert.That(extension.OwnerKind, Is.EqualTo(RepositoryEntityKind.DataType));
            Assert.That(extension.TypeName, Is.EqualTo("future:DataTypeExtension"));
            Assert.That(extension.RawContent?.RootName?.NamespaceUri, Is.EqualTo("urn:example:future"));
            Assert.That(extension.Children.Single().IsAttribute, Is.True);
            Assert.That(extension.Children.Single().Value, Is.EqualTo("new"));
            Assert.That(extension.References.Single().TargetIdentifier, Is.EqualTo("_futureType"));
            Assert.That(extension.Location.CharacterOffset, Is.EqualTo(4096));
        });
    }

    [Test]
    public void MutableRepositoryDefaultsAreIndependentBetweenInstances()
    {
        RepositorySnapshot firstSnapshot = new RepositorySnapshot();
        RepositorySnapshot secondSnapshot = new RepositorySnapshot();
        RegistrationMetadata firstRegistration = new RegistrationMetadata();
        RegistrationMetadata secondRegistration = new RegistrationMetadata();
        SemanticMarkupData firstMarkup = new SemanticMarkupData();
        SemanticMarkupData secondMarkup = new SemanticMarkupData();
        RepositoryUnknownData firstUnknown = new RepositoryUnknownData();
        RepositoryUnknownData secondUnknown = new RepositoryUnknownData();

        firstSnapshot.References.Add(new RepositoryReference());
        firstSnapshot.Provenance.RelatedArtifacts.Add(new ArtifactManifest());
        firstRegistration.Examples.Add("first");
        firstMarkup.Elements.Add(new SemanticMarkupData());
        firstUnknown.Children.Add(new RepositoryUnknownData());

        Assert.Multiple(() =>
        {
            Assert.That(secondSnapshot.References, Is.Empty);
            Assert.That(secondSnapshot.Provenance.RelatedArtifacts, Is.Empty);
            Assert.That(secondRegistration.Examples, Is.Empty);
            Assert.That(secondMarkup.Elements, Is.Empty);
            Assert.That(secondUnknown.Children, Is.Empty);
            Assert.That(firstSnapshot.Provenance, Is.Not.SameAs(secondSnapshot.Provenance));
            Assert.That(firstRegistration.Constraints, Is.Not.SameAs(secondRegistration.Constraints));
            Assert.That(firstMarkup.Elements, Is.Not.SameAs(secondMarkup.Elements));
            Assert.That(firstUnknown.Location, Is.Not.SameAs(secondUnknown.Location));
        });
    }
}
